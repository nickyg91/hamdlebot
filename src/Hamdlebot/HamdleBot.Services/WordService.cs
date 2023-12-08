using Hamdle.Cache;

namespace HamdleBot.Services;

public class WordService : IWordService
{
    private readonly ICacheService _cache;

    public WordService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task InsertWords()
    {
        if (await _cache.KeyExists("words"))
        {
            return;
        }

        var wordFilePath = "words.txt";
        var words = File.ReadLinesAsync(wordFilePath);
        var addTasks = new List<Task>();
        await foreach (var word in words)
        {
            addTasks.Add(_cache.AddToSet("words", word));
        }

        await Task.WhenAll(addTasks);
    }

    public async Task<string?> GetRandomWord()
    {
        return await _cache.GetRandomItemFromSet("words");
    }
}