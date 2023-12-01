using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using Hamdle.Cache;
using Hamdlebot.Core;
using Hamdlebot.Core.Extensions;
using HamdleBot.Services.Mediators;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HamdleBot.Services.Twitch;

public class TwitchChatService : ITwitchChatService
{
    private readonly ICacheService _cache;
    private readonly IWordService _wordService;
    private readonly AppConfigSettings _settings;
    private readonly IHamdleService _hamdleService;
    private readonly HamdleMediator _mediator;
    private ClientWebSocket? _socket;
    private CancellationToken _cancellationToken;
    private Regex _parseUserRegex = new("(:\\w+!)");

    public TwitchChatService(
        IOptions<AppConfigSettings> settings, 
        IWordService wordService, 
        ICacheService cache,
        IHamdleService hamdleService,
        HamdleMediator mediator)
    {
        _wordService = wordService;
        _settings = settings.Value;
        _cache = cache;
        _hamdleService = hamdleService;
        _mediator = mediator;
    }

    public async Task<ClientWebSocket> CreateWebSocket(CancellationToken token)
    {
        _socket = new ClientWebSocket();
        _cancellationToken = token;
        await _socket.ConnectAsync(new Uri("wss://irc-ws.chat.twitch.tv:443"), token);
        await InsertValidCommands();
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
            await _socket.SendAsync(capReqSegment, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage,
                token);
            await _socket.SendAsync("JOIN #hamhamReborn"u8.ToArray(), WebSocketMessageType.Text,
                WebSocketMessageFlags.EndOfMessage, token);
            await WriteMessage("hamdlebot has arrived Kappa");
        }

        _hamdleService.SendMessageToChat += Handle_Hamdle_Message!;
        return _socket;
    }

    private async void Handle_Hamdle_Message(object sender, string message)
    {
        await WriteMessage(message);
    }
    
    public async Task WriteMessage(string message)
    {
        var ircMessage = $"PRIVMSG #hamhamReborn :{message}";
        var msg = new ArraySegment<byte>(Encoding.UTF8.GetBytes(ircMessage));
        await _socket!.SendAsync(msg, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage,
            _cancellationToken);
    }

    // we can simplify this at some point as it is a bit of spaghetti.
    public async Task HandleMessages()
    {
        try
        {
            WebSocketReceiveResult result;
            using var ms = new MemoryStream();
            var messageBuffer = WebSocket.CreateClientBuffer(2048, 1024);
            while (_socket!.State == WebSocketState.Open)
            {
                do
                {
                    result = await _socket.ReceiveAsync(messageBuffer, _cancellationToken);
                    await ms.WriteAsync(messageBuffer.Array.AsMemory(messageBuffer.Offset, result.Count),
                        _cancellationToken);
                } while (_socket.State == WebSocketState.Open && !result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msg = Encoding.UTF8.GetString(ms.ToArray());
                    var chatMessage = msg.ToTwitchMessage();
                    if (chatMessage.Message.Contains("PING"))
                    {
                        await _socket.SendAsync("PONG :tmi.twitch.tv"u8.ToArray(), WebSocketMessageType.Text,
                            WebSocketMessageFlags.EndOfMessage, _cancellationToken);
                    }
                    
                    if (!IsSelf(chatMessage.DisplayName))
                    {
                        if (_hamdleService.IsHamdleVotingInProgress())
                        {
                            if (int.TryParse(chatMessage.Message, out var vote))
                            {
                                _hamdleService.SubmitVoteForGuess(chatMessage.DisplayName!, vote);
                            }
                        }

                        if (!_hamdleService.IsHamdleVotingInProgress()
                            && _hamdleService.IsHamdleSessionInProgress())
                        {
                                await _hamdleService.SubmitGuess(chatMessage.User!, chatMessage.Message);
                        }
                        else
                        {
                            if (chatMessage.Message.Contains("!#") && ShouldProcess(chatMessage.Message))
                            {
                                await ProcessCommand(chatMessage.Message);
                            }
                        }
                    }
                }
                ms.Seek(0, SeekOrigin.Begin);
                ms.Position = 0;
                ms.SetLength(0);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private bool ShouldProcess(string command)
    {
        return command.StartsWith("!#");
    }

    private bool IsSelf(string message)
    {
        var parsed = string.Join("", message.TakeWhile(x => x != '!'))?.Replace(":", "");
        return parsed == "hamdlebot" || parsed == "nightbot";
    }
    
    private async Task InsertValidCommands()
    {
        var validCommands = new List<string>
        {
            "!#commands",
            "!#random",
            "!#hamdle"
        };
        foreach (var command in validCommands)
        {
            await _cache.AddToSet("commands", command);
        }
    }

    private async Task<bool> IsValidCommand(string command)
    {
        return await _cache.ContainsMember("commands", command);
    }

    private async Task ProcessCommand(string command)
    {
        var isValidCommand = await IsValidCommand(command);
        if (!isValidCommand)
        {
            await WriteMessage("Invalid command! SirSad");
        }

        switch (command)
        {
            case "!#commands":
                await WriteMessage("Commands: !#<command> commands, random, hamdle");
                break;
            case "!#random":
                var word = await _wordService.GetRandomWord();
                await WriteMessage(word ?? "nooooo!");
                break;
            case "!#hamdle":
                await _cache.Subscriber.PublishAsync(new RedisChannel("startHamdleScene", RedisChannel.PatternMode.Auto), "start");
                break;
        }
    }
}