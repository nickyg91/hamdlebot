using System.Net.WebSockets;
using System.Text;

namespace HamdleBot.Services.Handlers;

public abstract class WebSocketHandlerBase(string url, CancellationToken cancellationToken)
{
    private readonly ClientWebSocket _socket = new();

    public event Func<Task>? Connected;
    public event Action? ReconnectStarted;
    public event Action<string>? MessageReceived;

    public async Task Connect()
    {
        var retryCount = 0;
        while(_socket.State != WebSocketState.Open && retryCount < 5)
        {
            try
            {
                await _socket.ConnectAsync(new Uri(url), cancellationToken);
                retryCount = 0;
            }
            catch (WebSocketException)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                await Task.Delay(delay, cancellationToken);
                ReconnectStarted?.Invoke();
            }
        }
        _ = Task.Run(StartListening, cancellationToken);
        Connected?.Invoke();
    }

    public async Task Disconnect()
    {
        if (_socket.State == WebSocketState.Open)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
        }
    }
    public async Task SendMessage(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }
    
    private async Task StartListening()
    {
        try
        {
            using var ms = new MemoryStream();
            while (_socket.State == WebSocketState.Open)
            {
                
                WebSocketReceiveResult result;
                do
                {
                    var messageBuffer = WebSocket.CreateClientBuffer(1024, 16);
                    result = await _socket.ReceiveAsync(messageBuffer, cancellationToken);
                    await ms.WriteAsync(messageBuffer.Array.AsMemory(messageBuffer.Offset, result.Count),
                        cancellationToken);
                } while (!result.EndOfMessage);
            
                if (result is { MessageType: WebSocketMessageType.Text })
                {
                    var message = Encoding.UTF8.GetString(ms.ToArray());
                    MessageReceived?.Invoke(message);
                }
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