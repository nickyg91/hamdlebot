namespace Hamdlebot.Core.SignalR.Clients.Hamdle;

public interface IHamdleHubClient : ISignalrHubClient
{
    Task ReceiveSelectedWord(string word);
    Task ReceiveGuess(string word);
    Task ReceiveResetState();
    Task ReceiveStartGuessTimer(int milliseconds);
    Task ReceiveStartVoteTimer(int milliseconds);
    Task ReceiveStartBetweenRoundTimer(int milliseconds);
}