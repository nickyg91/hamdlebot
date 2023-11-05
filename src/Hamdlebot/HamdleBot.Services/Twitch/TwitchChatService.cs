using System.Net.WebSockets;
using System.Text;
using Hamdlebot.Core;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.Extensions.Options;

namespace HamdleBot.Services.Twitch;

public class TwitchChatService : ITwitchChatService
{
    private readonly IHamdleWordService _wordService;
    private readonly AppConfigSettings _settings;
    private ClientWebSocket? _socket;
    private CancellationToken _cancellationToken;

    public TwitchChatService(IOptions<AppConfigSettings> settings, IHamdleWordService wordService)
    {
        _wordService = wordService;
        _settings = settings.Value;
    }
    public async Task<ClientWebSocket> CreateWebSocket(CancellationToken token)
    {
        _socket = new ClientWebSocket();
        _cancellationToken = token;
        await _socket.ConnectAsync(new Uri("wss://irc-ws.chat.twitch.tv:443"), token);
        
        if (_socket.State == WebSocketState.Open)
        {
            var capReq = $"CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands";
            var pass = $"PASS oauth:{_settings.TwitchConnectionInfo.ClientSecret}";
            var nick = "NICK hamdlebot";
            var passSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(pass));
            var nickSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(nick));
            var capReqSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(capReq));
            await _socket.SendAsync(passSegment, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, token);
            await _socket.SendAsync(nickSegment, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, token);
            await _socket.SendAsync(capReqSegment, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, token);
            await _socket.SendAsync("JOIN #hamhamReborn"u8.ToArray(), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, token);
            await WriteMessage("hamdlebot has arrived Kappa");
        }
        
        //register events
        _wordService.SendMessage += HamdleWordService_SendMessage!;
        
        return _socket;
    }

    public async Task WriteMessage(string message)
    {
        var ircMessage = $"PRIVMSG #hamhamReborn :{message}";
        var msg = new ArraySegment<byte>(Encoding.UTF8.GetBytes(ircMessage));
        await _socket!.SendAsync(msg, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, _cancellationToken);
    }
    
    public async Task HandleMessages()
    {
        try
        {
            while (_socket!.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    var messageBuffer = WebSocket.CreateClientBuffer(1024, 24);
                    result = await _socket.ReceiveAsync(messageBuffer, _cancellationToken);
                    await ms.WriteAsync(messageBuffer.Array.AsMemory(messageBuffer.Offset, result.Count),  _cancellationToken);
                } 
                while (_socket.State == WebSocketState.Open && !result.EndOfMessage);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msg = Encoding.UTF8.GetString(ms.ToArray());
                    if (msg.Contains("PING"))
                    {
                        await _socket.SendAsync("PONG :tmi.twitch.tv"u8.ToArray(), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, _cancellationToken);
                    }

                    if (!IsSelf(msg))
                    {
                        if (_wordService.IsHamdleVotingInProgress() && msg.Contains("PRIVMSG"))
                        {
                            var privmsg = msg.Split("PRIVMSG")[1];
                            var parsedVote =  privmsg.Split(":")[1].Trim();
                            //TODO submit vote
                        }
                        if (!_wordService.IsHamdleVotingInProgress() && _wordService.IsHamdleSessionInProgress() && msg.Contains("PRIVMSG"))
                        {
                            var privmsg = msg.Split("PRIVMSG")[1];
                            var parsedGuess =  privmsg.Split(":")[1].Trim();
                            if (!string.IsNullOrEmpty(parsedGuess))
                            {
                                await _wordService.SubmitGuess(parsedGuess);
                            }
                        }
                        else
                        {
                            string command = string.Empty;
                            if (msg.Contains(":!#"))
                            {
                                command = msg.Split("PRIVMSG")[1];
                                command = command.Split(":")[1].Trim();
                            }
                            if (ShouldProcess(command))
                            {
                                await _wordService.ProcessCommand(command);
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

    private bool ShouldProcess(string command)
    {
        return command.StartsWith("!#");
    }

    private async Task SendMessageReceived(string message)
    {
        await WriteMessage(message);
    }

    private async void HamdleWordService_SendMessage(object sender, string message)
    {
        await WriteMessage(message);
    }

    private bool IsSelf(string message)
    {
        var parsed = string.Join("", message.TakeWhile(x => x != '!'))?.Replace(":", "");
        return parsed == "hamdlebot";
    }
}