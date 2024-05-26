using Hamdle.Cache;
using Microsoft.Extensions.Logging;

namespace HamdleBot.Services;

public class WordService : IWordService
{
    private readonly ICacheService _cache;
    private readonly ILogger<WordService> _logger;

    public WordService(ICacheService cache, ILogger<WordService> logger)
    {
        _cache = cache;
        _logger = logger;
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
        _logger.Log(LogLevel.Information, "Words inserted into cache.");
    }

    public async Task<string?> GetRandomWord()
    {
        return await _cache.GetRandomItemFromSet("words");
    }
}