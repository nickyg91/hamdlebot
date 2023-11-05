namespace HamdleBot.Services;
public interface IHamdleWordService
{
    event EventHandler<string> SendMessage;
    Task InsertWords();
    Task RemoveWord(string word);
    Task AddWord(string word);
    Task<string?> GetRandomWord();
    Task InsertValidCommands();
    Task<bool> IsValidCommand(string command);
    Task ProcessCommand(string command);
    Task StartHamdleSession();
    bool IsHamdleSessionInProgress();
    bool IsHamdleVotingInProgress();
    Task SubmitGuess(string guess);
}