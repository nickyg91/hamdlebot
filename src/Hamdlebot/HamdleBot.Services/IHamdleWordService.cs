using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;

namespace HamdleBot.Services;
public interface IHamdleWordService
{
    event EventHandler<string> SendMessage;
    event EventHandler<OBSRequest<GetSceneItemListRequest>> SendGetSceneItemListRequestToObs;
    Task InsertWords();
    Task RemoveWord(string word);
    Task AddWord(string word);
    Task<string?> GetRandomWord();
    Task InsertValidCommands();
    Task<bool> IsValidCommand(string command);
    Task ProcessCommand(string command);
    Task StartHamdleRound();
    bool IsHamdleSessionInProgress();
    bool IsHamdleVotingInProgress();
    Task SubmitGuess(string username, string guess);
    void SubmitVoteForGuess(string username, int submission);
}