using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Hamdle.Cache;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using Hamdlebot.Models.OBS.ResponseTypes;
using HamdleBot.Services.Mediators;
using StackExchange.Redis;

namespace HamdleBot.Services.OBS;

public class ObsService : IObsService
{
    private ClientWebSocket? _socket;
    private CancellationToken _cancellationToken;
    private ICacheService _cache;

    public ObsService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task CreateWebSocket(CancellationToken cancellationToken)
    {
        _socket = new ClientWebSocket();
        _cancellationToken = cancellationToken;
        await _socket.ConnectAsync(new Uri("ws://localhost:4455"), _cancellationToken);
    }

    public async Task SendRequest<T>(ObsRequest<T> message) where T : class
    {
        var serializedJson = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var arrayBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(serializedJson));
        await _socket!.SendAsync(arrayBuffer, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage,
            _cancellationToken);
    }

    public ObsResponse<T>? ProcessMessage<T>(string message) where T : class
    {
        return !string.IsNullOrEmpty(message) ? JsonSerializer.Deserialize<ObsResponse<T>>(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }) : null;
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

    public async Task HandleMessages()
    {
        try
        {
            while (_socket!.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                var messageBuffer = WebSocket.CreateClientBuffer(2048, 1024);
                do
                {
                    result = await _socket.ReceiveAsync(messageBuffer, _cancellationToken);
                    await ms.WriteAsync(messageBuffer.Array.AsMemory(messageBuffer.Offset, result.Count),
                        _cancellationToken);
                } while (_socket.State == WebSocketState.Open && !result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msg = Encoding.UTF8.GetString(ms.ToArray());

                    if (!string.IsNullOrEmpty(msg))
                    {
                        var obj = JsonNode.Parse(msg)?.AsObject();
                        var opCode = obj?["op"]?.ToString();
                        if (opCode is "0" or "3")
                        {
                            await SendIdentifyRequest();
                        }

                        if (obj?["d"]?["requestType"]?.ToString()?.ToLower() == "getsceneitemlist")
                        {
                            var response = ProcessMessage<GetSceneItemListResponse>(msg);
                            if (response != null)
                            {
                                await GetHamdleScene(response.Response.ResponseData);
                            }
                        }
                    }
                }

                ms.Seek(0, SeekOrigin.Begin);
                ms.Position = 0;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task GetHamdleScene(GetSceneItemListResponse scenes)
    {
        var scene = scenes.SceneItems.FirstOrDefault(x => x.SourceName?.ToLower() == "hamdle");
        if (scene == null)
        {
            throw new KeyNotFoundException("Hamdle not found within set of scene items.");
        }

        var jsonString = JsonSerializer.Serialize(scene);
        await _cache.Subscriber.PublishAsync(
            new RedisChannel("onSceneRetrieved", RedisChannel.PatternMode.Auto), jsonString);
    }
}