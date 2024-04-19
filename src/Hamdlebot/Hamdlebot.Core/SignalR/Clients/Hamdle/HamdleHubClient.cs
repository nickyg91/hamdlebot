using Microsoft.AspNetCore.SignalR.Client;

namespace Hamdlebot.Core.SignalR.Clients.Hamdle;

public class HamdleHubClient : IHamdleHubClient
{
    private readonly HubConnection _hub;

    public HamdleHubClient(HubConnection hub)
    {
        _hub = hub;
    }
    public async Task SendSelectedWord(string word)
    {
        await _hub.InvokeAsync("SendSelectedWord", word);
    }

    public async Task SendGuess(string word)
    {
        await _hub.InvokeAsync("SendGuess", word);
    }

    public async Task ResetState()
    {
        await _hub.InvokeAsync("ResetState");
    }

    public async Task StartGuessTimer(int milliseconds)
    {
        await _hub.InvokeAsync("StartGuessTimer", milliseconds);
    }

    public async Task StartVoteTimer(int milliseconds)
    {
        await _hub.InvokeAsync("StartVoteTimer", milliseconds);
    }

    public async Task StartBetweenRoundTimer(int milliseconds)
    {
        await _hub.InvokeAsync("StartBetweenRoundTimer", milliseconds);
    }
}