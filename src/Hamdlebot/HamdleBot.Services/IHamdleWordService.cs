using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using Hamdlebot.Models.OBS.ResponseTypes;

namespace HamdleBot.Services;
public interface IHamdleWordService
{
    event EventHandler<string> SendMessage;
    event EventHandler<ObsRequest<GetSceneItemListRequest>> SendGetSceneItemListRequestToObs;
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
    void SetHamdleSceneItem(SceneItem item);
}