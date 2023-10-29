namespace HamdleBot.Services;

public interface IHamdleWordService
{
    Task InsertWords();
    Task<List<string>> GetAllWords();
    Task RemoveWord(string word);
    Task AddWord(string word);
    Task<string?> GetRandomWord();
    Task InsertValidCommands();
    Task<bool> IsValidCommand(string command);
    Task<string> ProcessCommand(string command);
}