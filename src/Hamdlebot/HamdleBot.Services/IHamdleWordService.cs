namespace HamdleBot.Services;
public interface IHamdleWordService
{
    event EventHandler<string> SendMessage;
    Task InsertWords();
    Task<List<string>> GetAllWords();
    Task RemoveWord(string word);
    Task AddWord(string word);
    Task<string?> GetRandomWord();
    Task InsertValidCommands();
    Task<bool> IsValidCommand(string command);
    Task ProcessCommand(string command);
    Task StartHamdleSession();
    bool IsHamdleSessionInProgress();
    Task SubmitGuess(string guess);
}