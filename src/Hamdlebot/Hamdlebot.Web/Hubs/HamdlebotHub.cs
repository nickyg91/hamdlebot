using Microsoft.AspNetCore.SignalR;

namespace Hamdlebot.Web.Hubs;

public class HamdlebotHub : Hub
{
    public async Task SendSelectedWord(string word)
    {
        await Clients.All.SendAsync("SendSelectedWord", word);
    }

    public async Task SendGuess(string word)
    {
        await Clients.All.SendAsync("SendGuess", word);
    }

    public async Task ResetState()
    {
        await Clients.All.SendAsync("ResetState");
    }

    public async Task StartGuessTimer(int milliseconds)
    {
        await Clients.All.SendAsync("StartGuessTimer", milliseconds);
    }
    
    public async Task StartVoteTimer(int milliseconds)
    {
        await Clients.All.SendAsync("StartVoteTimer", milliseconds);
    }

    public async Task StartBetweenRoundTimer(int milliseconds)
    {
        await Clients.All.SendAsync("StartBetweenRoundTimer", milliseconds);
    }
}