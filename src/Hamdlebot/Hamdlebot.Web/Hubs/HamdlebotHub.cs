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
}