using System.Net.WebSockets;
using System.Text;
using Hamdlebot.Core.Extensions;
using Hamdlebot.Core.Models;
using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients.Logging;

namespace HamdleBot.Services.Handlers;

public class WebSocketHandler
{
    private ClientWebSocket _socket;
    private CancellationToken _cancellationToken;
    private readonly IBotLogClient _logClient;
    private string _url;
    public event Action<TwitchMessage>? MessageReceived;
    public event Func<Task>? Connected;
    
    public WebSocketHandler(string url, CancellationToken cancellationToken, IBotLogClient logClient)
    {
        _socket = new ClientWebSocket();
        _cancellationToken = cancellationToken;
        _logClient = logClient;
        _url = url;
    }
    
    public async Task Connect()
    {
        var retryCount = 0;
        while(_socket.State != WebSocketState.Open && retryCount < 5)
        {
            try
            {
                await _socket.ConnectAsync(new Uri(_url), _cancellationToken);
                await _logClient.LogMessage(new LogMessage("Attempting reconnect to Twitch Chat.", DateTime.UtcNow, SeverityLevel.Warning));
                retryCount = 0;
            }
            catch (WebSocketException)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                await Task.Delay(delay, _cancellationToken);
            }
        }
        await _socket.ConnectAsync(new Uri(_url), _cancellationToken);
        StartListening();
        Connected?.Invoke();
    }

    public async Task SendMessage(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationToken);
    }
    
    private async void StartListening()
    {
        try
        {
            using var ms = new MemoryStream();
            var messageBuffer = WebSocket.CreateClientBuffer(2048, 1024);
            while (_socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(messageBuffer, _cancellationToken);
                    await ms.WriteAsync(messageBuffer.Array.AsMemory(messageBuffer.Offset, result.Count),
                        _cancellationToken);
                } while (_socket.State == WebSocketState.Open && !result.EndOfMessage);
            
                if (result is not { MessageType: WebSocketMessageType.Text })
                {
                    return;
                }
                var message = Encoding.UTF8.GetString(ms.ToArray());
                var twitchChatMessage = message.ToTwitchMessage();
                MessageReceived?.Invoke(twitchChatMessage);
                ms.Seek(0, SeekOrigin.Begin);
                ms.Position = 0;
                ms.SetLength(0);
            }
        }
        catch (WebSocketException e)
        {
            if (e.Message ==
                "The remote party closed the WebSocket connection without completing the close handshake.")
            {
                await Connect();
            }
        }
    }
}