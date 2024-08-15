using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients.Logging;
using Hamdlebot.Data.Contexts.Hamdlebot;
using Hamdlebot.Models;
using Hamdlebot.Models.ViewModels;
using HamdleBot.Services.Consumers;
using HamdleBot.Services.Twitch.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HamdleBot.Services.Twitch;

public class TwitchChatService : ITwitchChatService
{
    private readonly Dictionary<long, TwitchChannel> _channels = new();
    private readonly ICacheService _cache;
    private readonly IBotLogClient _logClient;
    private CancellationToken? _cancellationToken;
    private readonly RedisChannel _botTokenChannel;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBus _bus;
    private readonly IOptions<AppConfigSettings> _settings;
    public TwitchChatService(
        ICacheService cache,
        IBotLogClient logClient,
        IServiceProvider serviceProvider,
        IBus bus,
        IOptions<AppConfigSettings> settings)
    {
        _cache = cache;
        _logClient = logClient;
        _serviceProvider = serviceProvider;
        _bus = bus;
        _settings = settings;
        _botTokenChannel = new RedisChannel(RedisChannelType.BotTwitchToken, RedisChannel.PatternMode.Auto);
        SetupSubscriptions();
    }

    public async Task JoinBotToChannel(Channel channel)
    {
        
        if (_channels.ContainsKey(channel.TwitchUserId))
        {
            return;
        }
        var oauthToken = await _cache.GetItem(CacheKeyType.TwitchOauthToken);

        using var scope = _serviceProvider.CreateScope();
        
        var hamdleHub = scope.ServiceProvider.GetKeyedService<HubConnection>(KeyedServiceValues.HamdleHub);
        var channelNotificationsHub =
            scope.ServiceProvider.GetKeyedService<HubConnection>(KeyedServiceValues.ChannelNotificationsHub);
        var obsSettings = await _cache.GetObject<ObsSettings>($"{CacheKeyType.UserObsSettings}:{channel.TwitchUserId}");
        
        var aggregate = new TwitchChannelAggregate(
            _settings.Value.TwitchConnectionInfo!.HamdlebotUserId,
            _settings.Value.TwitchConnectionInfo!.ClientId!,
            oauthToken ?? "",
            obsSettings,
            channel,
            hamdleHub!,
            channelNotificationsHub!);
        
        var twitchChannel = 
            new TwitchChannel(aggregate, _cache, _cancellationToken!.Value);
        
        _channels.Add(channel.TwitchUserId, twitchChannel);
        
        _bus.ConnectReceiveEndpoint($"{MassTransitReceiveEndpoints.TwitchChannelSettingsUpdatedConsumer}-{channel.TwitchUserId}", cfg =>
        {
            cfg.Consumer(() => new TwitchChannelSettingsUpdatedConsumer(twitchChannel));
            cfg.UseMessageRetry(rty =>
            {
                rty.Immediate(1);
            });
        });
        
        if (oauthToken is null)
        {
            await _logClient.LogMessage(
                new LogMessage($"No valid OAuth token found. Cannot join channel {channel.TwitchUserId}.", 
                    DateTime.UtcNow,
                    SeverityLevel.Error));
            return;
        }
        await twitchChannel.Connect();
    }

    public async Task LeaveChannel(long twitchUserId)
    {
        if (_channels.TryGetValue(twitchUserId, out var channel))
        {
            await channel.LeaveChannel();
            _channels.Remove(twitchUserId);
        }
    }

    public async Task JoinExistingChannels()
    {
        // for now - this is not performant AT ALL when dealing with large datasets.
        using var scope = _serviceProvider.CreateScope();
        var dbCtx = scope.ServiceProvider.GetRequiredService<HamdlebotContext>();
        var channels = await dbCtx.BotChannels.Include(x => x.BotChannelCommands).AsNoTracking().ToListAsync();
        foreach (var channel in channels)
        {
            var vmChannel = new Channel(channel);
            await JoinBotToChannel(vmChannel);
        }
    }

    public void SetCancellationToken(CancellationToken token)
    {
        _cancellationToken = token;
    }

    private async Task UpdateToken()
    {
        var oauthToken = await _cache.GetItem(CacheKeyType.TwitchOauthToken);

        if (oauthToken is null)
        {
            await _logClient.LogMessage(new LogMessage("OAuth token was null. Bailing early from UpdateToken().", DateTime.UtcNow,
                SeverityLevel.Info));
            return;
        }
        
        await _logClient.LogMessage(new LogMessage($"Updating token for {_channels.Count} channels.", DateTime.UtcNow,
            SeverityLevel.Info));

        foreach (var channel in _channels.Values)
        {
            await channel.Reauthenticate(oauthToken);
        }
    }
    
    private void SetupSubscriptions()
    {
        _cache.Subscriber.Subscribe(_botTokenChannel).OnMessage(
            async _ =>
            {
                await _logClient.LogMessage(new LogMessage("Updated token received.", DateTime.UtcNow,
                    SeverityLevel.Info));
                await UpdateToken();
            });
    }
}