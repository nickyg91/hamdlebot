using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using Hamdlebot.Models.OBS.ResponseTypes;

namespace HamdleBot.Services.OBS;

public class OBSService : IOBSService
{
    private readonly IHamdleWordService _wordService;
    private ClientWebSocket _socket;
    private CancellationToken _cancellationToken;

    public OBSService(IHamdleWordService wordService)
    {
        _wordService = wordService;
        _wordService.SendGetSceneItemListRequestToObs += OBS_Send_GetSceneItemsList!;
    }
    
    private async void OBS_Send_GetSceneItemsList(object sender, OBSRequest<GetSceneItemListRequest> request)
    {
        await SendRequest(request);
    }
    
    public async Task CreateWebSocket(CancellationToken cancellationToken)
    {
        _socket = new ClientWebSocket();
        _cancellationToken = cancellationToken;
        await Connect();
    }

    private async Task Connect()
    {
        await _socket.ConnectAsync(new Uri("ws://localhost:4455"), _cancellationToken);
    }

    public async Task SendRequest<T>(OBSRequest<T> message) where T : class
    {
        var serializedJson = JsonSerializer.Serialize(message);
        var arrayBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(serializedJson));
        await _socket.SendAsync(arrayBuffer, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, _cancellationToken);
    }

    public async Task<OBSResponse<T>?> ProcessMessage<T>(string message) where T : class
    {
        if (!string.IsNullOrEmpty(message))
        {
            return JsonSerializer.Deserialize<OBSResponse<T>>(message);
        }

        return null;
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
                    await ms.WriteAsync(messageBuffer.Array.AsMemory(messageBuffer.Offset, result.Count),  _cancellationToken);
                } 
                while (_socket.State == WebSocketState.Open && !result.EndOfMessage);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msg = Encoding.UTF8.GetString(ms.ToArray());
                    
                    if (!string.IsNullOrEmpty(msg))
                    {
                        var obj = JsonNode.Parse(msg)?.AsObject();
                        if (obj?["requestType"]?.ToString()?.ToLower() == "getsceneitemlist")
                        {
                            var getSceneItemList =
                                JsonSerializer.Deserialize<OBSResponse<GetSceneItemListResponse>>(msg);
                            Console.WriteLine(string.Join(", ", getSceneItemList?.D?.SceneItems));
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
}