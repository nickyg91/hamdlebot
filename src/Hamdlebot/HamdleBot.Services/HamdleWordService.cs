using Hamdle.Cache;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using HamdleBot.Services.Hamdle;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services;

public class HamdleWordService : IHamdleWordService
{
    private readonly ICacheService _cache;
    private readonly HubConnection _signalRHub;
    private HamdleContext _hamdleContext;

    public HamdleWordService(ICacheService cache, HubConnection signalRHub)
    {
        _cache = cache;
        _signalRHub = signalRHub;
    }

    public event EventHandler<string>? SendMessage;
    public event EventHandler<OBSRequest<GetSceneItemListRequest>>? SendGetSceneItemListRequestToObs;

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
            "!#hamdle"
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

    public async Task ProcessCommand(string command)
    {
        var isValidCommand = await IsValidCommand(command);
        if (!isValidCommand)
        {
            SendMessage?.Invoke(this, "Invalid command! SirSad");
        }

        switch (command)
        {
            case "!#commands":
                SendMessage?.Invoke(this, "Commands: !#commands, !#random, !#hamdle");
                break;
            case "!#random":
                var word = await GetRandomWord();
                SendMessage?.Invoke(this, word ?? "nooooo!");
                break;
            case "!#hamdle":
                if (_hamdleContext?.IsRoundInProgress ?? false)
                {
                    await StartHamdleRound();
                }

                break;
        }
    }

    public async Task StartHamdleRound()
    {
        _hamdleContext = new HamdleContext(_cache, _signalRHub, SendMessage);
        await _hamdleContext.StartGuesses();
    }

    public bool IsHamdleSessionInProgress()
    {
        return _hamdleContext?.IsRoundInProgress ?? false;
    }

    public bool IsHamdleVotingInProgress()
    {
        return _hamdleContext?.IsInVotingState ?? false;
    }
    public async Task SubmitGuess(string username, string guess)
    {
        await _hamdleContext.SubmitGuess(username, guess);
    }

    public void SubmitVoteForGuess(string username, int submission)
    {
        _hamdleContext.SubmitVoteForGuess(username, submission);
    }
}