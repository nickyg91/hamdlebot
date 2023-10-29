using Hamdle.Cache;

namespace HamdleBot.Services;

public class HamdleWordService : IHamdleWordService
{
    private readonly ICacheService _cache;

    public HamdleWordService(ICacheService cache)
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

    public async Task<List<string>> GetAllWords()
    {
        throw new NotImplementedException();
    }

    public async Task RemoveWord(string word)
    {
        throw new NotImplementedException();
    }

    public async Task AddWord(string word)
    {
        throw new NotImplementedException();
    }

    public async Task<string?> GetRandomWord()
    {
        return await _cache.GetRandomItemFromSet("words");
    }

    public async Task InsertValidCommands()
    {
        var validCommands = new List<string>
        {
            "!#commands",
            "!#random",
        };
        foreach (var command in validCommands)
        {
            await _cache.AddToSet("commands", command);
        }
    }

    public async Task<bool> IsValidCommand(string command)
    {
        return await _cache.ContainsMember("commands", command);
    }

    public async Task<string> ProcessCommand(string command)
    {
        var isValidCommand = await IsValidCommand(command);
        if (!isValidCommand)
        {
            return "Invalid command! SirSad";
        }

        var msg = command switch
        {
            "!#commands" => "Commands: !#commands, !#random",
            "!#random" => await GetRandomWord(),
            _ => "Unknown command."
        };

        return msg!;
    }
}