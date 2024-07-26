using System.Net.WebSockets;
using System.Text;

namespace HamdleBot.Services.Handlers;

public abstract class WebSocketHandlerBase
{
    private readonly ClientWebSocket _socket = new();
    private readonly string _url;
    private readonly CancellationToken _cancellationToken;
    private readonly byte _maxReconnectAttempts;
    public event Action? Connected;
    public event Action? ReconnectStarted;
    public event Action<string>? MessageReceived;
    public event Action? OnFault;
    public event Action? OnDisconnect;
    public WebSocketState State => _socket.State;
    protected CancellationToken CancellationToken => _cancellationToken;
    protected WebSocketHandlerBase(string url, CancellationToken cancellationToken, byte maxReconnectAttempts)
    {
        _url = url;
        _cancellationToken = cancellationToken;
        _maxReconnectAttempts = maxReconnectAttempts;
    }
    
    public async Task Connect()
    {
        var retryCount = 0;
        while(_socket.State != WebSocketState.Open
              && retryCount < _maxReconnectAttempts)
        {
            try
            {
                await _socket.ConnectAsync(new Uri(_url), _cancellationToken);
                retryCount = 0;
            }
            catch (WebSocketException e)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                await Task.Delay(delay, _cancellationToken);
                ReconnectStarted?.Invoke();
                retryCount++;
            }
        }
        if (retryCount >= _maxReconnectAttempts)
        {
            OnFault?.Invoke();
            throw new WebSocketException($"Failed to connect to the websocket at {_url}.");
        }
        _ = Task.Run(StartListening, _cancellationToken);
        Connected?.Invoke();
    }

    public async Task Disconnect()
    {
        if (_socket.State == WebSocketState.Open)
        {
            await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cancellationToken);
        }
        OnDisconnect?.Invoke();
    }
    public async Task SendMessage(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationToken);
    }

    public void RemoveEvents()
    {
        Connected = null;
        ReconnectStarted = null;
        MessageReceived = null;
        OnFault = null;
        OnDisconnect = null;
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
                    result = await _socket.ReceiveAsync(messageBuffer, _cancellationToken);
                    await ms.WriteAsync(messageBuffer.Array.AsMemory(messageBuffer.Offset, result.Count),
                        _cancellationToken);
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
            OnFault?.Invoke();
        }
    }
}