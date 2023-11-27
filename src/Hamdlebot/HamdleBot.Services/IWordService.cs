namespace HamdleBot.Services;
public interface IWordService
{
    Task InsertWords();
    // Task RemoveWord(string word);
    // Task AddWord(string word);
    Task<string?> GetRandomWord();
}