using System.Net.WebSockets;
using System.Text;

namespace HamdleBot.Services.Handlers;

public abstract class WebSocketHandlerBase
{
    private ClientWebSocket _socket;
    private CancellationToken _cancellationToken;
    private string _url;

    protected event Func<Task> Connected;
    protected event Action? ReconnectStarted;
    public event Action<string>? MessageReceived;
    protected bool IsConnected => _socket.State == WebSocketState.Open;
    
    protected WebSocketHandlerBase(string url, CancellationToken cancellationToken)
    {
        _socket = new ClientWebSocket();
        _cancellationToken = cancellationToken;
        _url = url;
    }

    protected async Task Connect()
    {
        var retryCount = 0;
        while(_socket.State != WebSocketState.Open && retryCount < 5)
        {
            try
            {
                await _socket.ConnectAsync(new Uri(_url), _cancellationToken);
                retryCount = 0;
            }
            catch (WebSocketException)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                await Task.Delay(delay, _cancellationToken);
                ReconnectStarted?.Invoke();
            }
        }
        Task.Run(StartListening, _cancellationToken);
        Connected?.Invoke();
    }

    protected async Task Disconnect()
    {
        if (_socket.State == WebSocketState.Open)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cancellationToken);
        }
    }
    protected async Task SendMessage(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationToken);
    }
    private async Task StartListening()
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
                MessageReceived?.Invoke(message);
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