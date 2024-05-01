using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients.Logging;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using Hamdlebot.Models.OBS.ResponseTypes;
using HamdleBot.Services.Handlers;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HamdleBot.Services.OBS;

public class ObsService : IObsService
{
    private ObsWebSocketHandler? _socket;
    private CancellationToken? _cancellationToken;
    private readonly ICacheService _cache;
    private readonly IBotLogClient _logClient;
    private readonly ObsSettings _obsSettings;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    private readonly RedisChannel _sceneReceivedChannel;
    
    
    public ObsService(
        ICacheService cache, 
        IOptions<AppConfigSettings> settings,
        IBotLogClient logClient)
    {
        _cache = cache;
        _logClient = logClient;
        _obsSettings = settings.Value.ObsSettingsOptions!;
        _sceneReceivedChannel = new RedisChannel(RedisChannelType.OnSceneReceived, RedisChannel.PatternMode.Auto);
    }

    public async Task CreateWebSocket(CancellationToken cancellationToken)
    {
        _cancellationToken ??= cancellationToken;
        _socket ??= new ObsWebSocketHandler(_obsSettings.SocketUrl!, _cancellationToken.Value);
        
        _socket.Connected += async () =>
        {
            await _logClient.LogMessage(new LogMessage("Connected to OBS websocket", DateTime.UtcNow, SeverityLevel.Info));
        };
        _socket.ReconnectStarted += async () =>
        {
            await _logClient.LogMessage(new LogMessage("Reconnecting to OBS websocket", DateTime.UtcNow, SeverityLevel.Info));
        };
        _socket.MessageReceived += async message =>
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            var obj = JsonNode.Parse(message)?.AsObject();
            var opCode = obj?["op"]?.ToString();
            if (opCode is "0" or "3")
            {
                await SendIdentifyRequest();
            }

            if (obj?["d"]?["requestType"]?.ToString().ToLower() != "getsceneitemlist")
            {
                return;
            }
                
            var response = ProcessMessage<GetSceneItemListResponse>(message);
            if (response != null)
            {
                await GetHamdleScene(response.Response.ResponseData);
            }
        };
        await _socket.Connect();
    }

    public async Task SendRequest<T>(ObsRequest<T> message) where T : class
    {
        var serializedJson = JsonSerializer.Serialize(message, _jsonOptions);
        await _logClient.LogMessage(new LogMessage($"Request sent to OBS for {message.RequestData?.RequestType ?? "scene"}." , DateTime.UtcNow, SeverityLevel.Info));
        await _socket!.SendMessage(serializedJson);
    }

    private ObsResponse<T>? ProcessMessage<T>(string message) where T : class
    {
        return !string.IsNullOrEmpty(message) ? JsonSerializer.Deserialize<ObsResponse<T>>(message, _jsonOptions) : null;
    }

    private async Task SendIdentifyRequest()
    {
        var req = new ObsRequest<IdentifyRequest>
        {
            RequestData = new RequestWrapper<IdentifyRequest>
            {
                RpcVersion = 1,
            },
            Op = OpCodeType.Identify
        };
        await SendRequest(req);
    }

    private async Task GetHamdleScene(GetSceneItemListResponse scenes)
    {
        var scene = scenes.SceneItems.FirstOrDefault(x => x.SourceName.ToLower().Equals(_obsSettings.HamdleSourceName, StringComparison.CurrentCultureIgnoreCase));
        if (scene == null)
        {
            throw new KeyNotFoundException("Hamdle not found within set of scene items.");
        }

        var jsonString = JsonSerializer.Serialize(scene);
        await _cache.Subscriber.PublishAsync(_sceneReceivedChannel, jsonString);
    }
}