using Hamdlebot.Core.Extensions;
using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using Hamdlebot.Models;
using HamdleBot.Services.Handlers;

namespace HamdleBot.Services.Twitch;

public class TwitchChannel : IObserver<string>
{
    private readonly BotChannel _botChannel;
    private TwitchChatWebSocketHandler _webSocketHandler;
    private string _botAccessToken;
    
    public TwitchChannel(
        BotChannel channel,
        string url,
        string botAccessToken,
        CancellationToken cancellationToken)
    {
        _botAccessToken = botAccessToken;
        _botChannel = channel;
        _webSocketHandler = 
            new TwitchChatWebSocketHandler
            (
                url, 
                cancellationToken, 
                _botChannel.TwitchChannelName, 
                3
            );
        SetupEvents();
    }
    
    private void SetupEvents()
    {
        _webSocketHandler.MessageReceived += async message =>
        {
            await OnMessageReceived(message);
        };
        _webSocketHandler.Connected += async () =>
        {
            await Authenticate();
        };
    }

    public async Task Authenticate()
    {
        await AuthenticateInternal();
    }

    private async Task Reauthenticate(string botAccessToken)
    {
        _botAccessToken = botAccessToken;
        await AuthenticateInternal();
    }

    private async Task AuthenticateInternal()
    {
        var capReq = $"CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands";
        var pass = $"PASS oauth:{_botAccessToken}";
        var nick = "NICK hamdlebot";
        await _webSocketHandler!.SendMessage(pass);
        await _webSocketHandler.SendMessage(nick);
        await _webSocketHandler.SendMessage(capReq);
        await Task.Delay(3000);
        await _webSocketHandler.JoinChannel();
    }
    
    private async Task OnMessageReceived(string message)
    {
        if (message.IsPingMessage())
        {
            await _webSocketHandler.SendMessage("PONG :tmi.twitch.tv");
        }
        
        var ircMessage = message.ToTwitchMessage();
        if (ircMessage.IsBot())
        {
            return;
        }
        
        if (ircMessage.IsCommand)
        {
            var command = _botChannel.BotChannelCommands.FirstOrDefault(x => x.Command == ircMessage.Message);
            if (command != null)
            {
                await _webSocketHandler.SendMessageToChat(command.Response);
            }
        }
    }

    public void OnCompleted()
    {
        //throw new NotImplementedException();
    }

    public void OnError(Exception error)
    {
        //throw new NotImplementedException();
    }

    public void OnNext(string botAccessToken)
    {
        _ = Task.Run(async () => await Reauthenticate(botAccessToken));
    }
}