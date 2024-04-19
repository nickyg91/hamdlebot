using Hamdlebot.Core.SignalR.Clients.Hamdle;
using Microsoft.AspNetCore.SignalR;

namespace Hamdlebot.Web.Hubs;

public class HamdlebotHub : Hub<IHamdleHubClient>
{
    public async Task SendSelectedWord(string word)
    {
        await Clients.All.SendSelectedWord(word);
    }

    public async Task SendGuess(string word)
    {
        await Clients.All.SendGuess(word);
    }

    public async Task ResetState()
    {
        await Clients.All.ResetState();
    }

    public async Task StartGuessTimer(int milliseconds)
    {
        await Clients.All.StartGuessTimer(milliseconds);
    }
    
    public async Task StartVoteTimer(int milliseconds)
    {
        await Clients.All.StartVoteTimer(milliseconds);
    }

    public async Task StartBetweenRoundTimer(int milliseconds)
    {
        await Clients.All.StartBetweenRoundTimer(milliseconds);
    }
}