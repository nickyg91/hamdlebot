namespace HamdleBot.Services;

public interface IHamdleService
{
    bool IsHamdleSessionInProgress();
    bool IsHamdleVotingInProgress();
    Task<bool> SubmitGuess(string username, string guess);
    void SubmitVoteForGuess(string username, int submission);
    event EventHandler<string> SendMessageToChat;
}