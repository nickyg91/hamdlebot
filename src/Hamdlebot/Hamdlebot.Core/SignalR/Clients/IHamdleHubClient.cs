namespace Hamdlebot.Core.SignalR.Clients;

public interface IHamdleHubClient : ISignalrHubClient
{
    Task SendSelectedWord(string word);
    Task SendGuess(string word);
    Task ResetState();
    Task StartGuessTimer(int milliseconds);
    Task StartVoteTimer(int milliseconds);
    Task StartBetweenRoundTimer(int milliseconds);
}