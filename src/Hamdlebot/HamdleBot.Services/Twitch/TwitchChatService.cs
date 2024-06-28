using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients.Logging;
using Hamdlebot.Data.Contexts.Hamdlebot;
using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using Hamdlebot.Models;
using HamdleBot.Services.Handlers;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HamdleBot.Services.Twitch;

public class TwitchChatService : ITwitchChatService
{
    private const string TwitchWebSocketUrl = "wss://irc-ws.chat.twitch.tv:443";
    private readonly Dictionary<long, TwitchChannel> _channels = new();
    private readonly ICacheService _cache;
    private readonly ITwitchIdentityApiService _identityApiService;
    private readonly IBotLogClient _logClient;
    private CancellationToken? _cancellationToken;
    private readonly RedisChannel _botTokenChannel;
    private readonly TwitchAuthTokenUpdateHandler _authTokenUpdateHandler;
    private readonly IServiceProvider _serviceProvider;
    public TwitchChatService(
        ICacheService cache,
        ITwitchIdentityApiService identityApiService,
        IBotLogClient logClient,
        TwitchAuthTokenUpdateHandler authTokenUpdateHandler,
        IServiceProvider serviceProvider)
    {
        _cache = cache;
        _identityApiService = identityApiService;
        _logClient = logClient;
        _authTokenUpdateHandler = authTokenUpdateHandler;
        _serviceProvider = serviceProvider;
        _botTokenChannel = new RedisChannel(RedisChannelType.BotTwitchToken, RedisChannel.PatternMode.Auto);
        SetupSubscriptions();
    }

    public async Task JoinBotToChannel(BotChannel channel)
    {
        if (_channels.ContainsKey(channel.TwitchUserId))
        {
            return;
        }
        var oauthToken = await _cache.GetItem(CacheKeyType.TwitchOauthToken);
        if (oauthToken is null)
        {
            await _logClient.LogMessage(
                new LogMessage($"No valid OAuth token found. Cannot join channel {channel.TwitchUserId}.", 
                DateTime.UtcNow,
                SeverityLevel.Error));
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var hamdleHub = scope.ServiceProvider.GetKeyedService<HubConnection>(KeyedServiceValues.HamdleHub);
        var twitchChannel = 
            new TwitchChannel(channel, TwitchWebSocketUrl, oauthToken, _cache, hamdleHub!, _cancellationToken!.Value);
        twitchChannel.Connect();
        _authTokenUpdateHandler.Subscribe(twitchChannel);
        _channels.Add(channel.TwitchUserId, twitchChannel);
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
        var token = await Authenticate();
        if (token == null)
        {
            await _logClient.LogMessage(new LogMessage("Failed to authenticate with Twitch. No valid token found.",
                DateTime.UtcNow, SeverityLevel.Error));
            return;
        }
        // for now - this is not performant AT ALL when dealing with large datasets.
        using var scope = _serviceProvider.CreateScope();
        var dbCtx = scope.ServiceProvider.GetRequiredService<HamdlebotContext>();
        var channels = await dbCtx.BotChannels.Include(x => x.BotChannelCommands).AsNoTracking().ToListAsync();
        foreach (var channel in channels)
        {
            await JoinBotToChannel(channel);
        }
    }

    public async Task ConnectToObs(long twitchUserId)
    {
        if (_channels.TryGetValue(twitchUserId, out var channel))
        {
            await channel.ConnectToObs();
        }
    }

    public async Task DisconnectFromObs(long twitchUserId)
    {
        if (_channels.TryGetValue(twitchUserId, out var channel))
        {
            await channel.DisconnectFromObs();
        }
    }

    public void SetCancellationToken(CancellationToken token)
    {
        _cancellationToken = token;
    }

    public void UpdateChannelSettings(BotChannel channel)
    {
        if (_channels.TryGetValue(channel.TwitchUserId, out var twitchChannel))
        {
            twitchChannel.UpdateChannelSettings(channel);
        }
    }

    private async Task<ClientCredentialsTokenResponse?> Authenticate()
    {
        ClientCredentialsTokenResponse tokenResponse;
        var oauthToken = await _cache.GetItem(CacheKeyType.TwitchOauthToken);
        var refreshToken = await _cache.GetItem(CacheKeyType.TwitchRefreshToken);
        
        if (oauthToken != null && refreshToken != null)
        {
            await _logClient.LogMessage(new LogMessage("Valid OAuth token found.", DateTime.UtcNow,
                SeverityLevel.Info));
            return new ClientCredentialsTokenResponse
            {
                AccessToken = oauthToken,
                RefreshToken = refreshToken
            };
        }

        if (oauthToken == null || refreshToken == null)
        {
            await _logClient.LogMessage(new LogMessage("Valid OAuth token not found.", DateTime.UtcNow,
                SeverityLevel.Info));
            return null;
        }

        await _logClient.LogMessage(new LogMessage("Fetching new twitch OAuthToken.", DateTime.UtcNow,
            SeverityLevel.Info));
        tokenResponse = await _identityApiService.RefreshToken(refreshToken);
        await _cache.AddItem(CacheKeyType.TwitchOauthToken, tokenResponse.AccessToken,
            TimeSpan.FromSeconds(tokenResponse.ExpiresIn));
        await _cache.AddItem(CacheKeyType.TwitchRefreshToken, tokenResponse.RefreshToken, TimeSpan.FromDays(30));
        _authTokenUpdateHandler.UpdateToken(tokenResponse.AccessToken);
        return tokenResponse;
    }

    private void SetupSubscriptions()
    {
        _cache.Subscriber.Subscribe(_botTokenChannel).OnMessage(
            async _ =>
            {
                await Authenticate();
            });
    }
}