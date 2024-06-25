using Hamdlebot.Core.SignalR.Clients.Hamdle;
using Microsoft.AspNetCore.SignalR;

namespace Hamdlebot.Web.Hubs;

public class HamdlebotHub : Hub<IHamdleHubClient>
{
    public override async Task OnConnectedAsync()
    {
        if (Context.GetHttpContext() != null && Context.GetHttpContext()!.Request.Query.ContainsKey("twitchUserId"))
        {
            var twitchUserId = Context.GetHttpContext()!.Request.Query["twitchUserId"]!;
            await Groups.AddToGroupAsync(Context.ConnectionId, twitchUserId!);
        }
        await base.OnConnectedAsync();
    }

    public async Task SendSelectedWord(string word, string twitchUserId)
    {
        await Clients.Group(twitchUserId).ReceiveSelectedWord(word);
    }

    public async Task SendGuess(string word, string twitchUserId)
    {
        await Clients.Group(twitchUserId).ReceiveGuess(word);
    }

    public async Task SendResetState(string twitchUserId)
    {
        await Clients.Group(twitchUserId).ReceiveResetState();
    }

    public async Task StartGuessTimer(int milliseconds, string twitchUserId)
    {
        await Clients.Group(twitchUserId).ReceiveStartGuessTimer(milliseconds);
    }
    
    public async Task StartVoteTimer(int milliseconds, string twitchUserId)
    {
        await Clients.Group(twitchUserId).ReceiveStartVoteTimer(milliseconds);
    }

    public async Task StartBetweenRoundTimer(int milliseconds, string twitchUserId)
    {
        await Clients.Group(twitchUserId).ReceiveStartBetweenRoundTimer(milliseconds);
    }
}