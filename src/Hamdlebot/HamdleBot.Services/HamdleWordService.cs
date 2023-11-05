using System.Timers;
using Hamdle.Cache;
using HamdleBot.Services.Twitch.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services;

public class HamdleWordService : IHamdleWordService
{
    private readonly ICacheService _cache;
    private readonly HubConnection _signalRHub;
    private bool _isHamdleInProgress;
    private System.Timers.Timer _guessTimer { get; set; }
    private readonly HashSet<string> _guesses;
    private string? _currentWord;
    
    public HamdleWordService(ICacheService cache, HubConnection signalRHub)
    {
        _cache = cache;
        _signalRHub = signalRHub;
        _guesses = new HashSet<string>();
    }

    public event EventHandler<string>? SendMessage;

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

    public async Task ProcessCommand(string command)
    {
        var isValidCommand = await IsValidCommand(command);
        if (!isValidCommand)
        {
            SendMessage?.Invoke(this, "Invalid command! SirSad");
        }

        string msg = string.Empty;
        switch (command)
        {
            case "!#commands":
                msg = "Commands: !#commands, !#random, !#hamdle";
                break;
            case "!#random":
                msg = await GetRandomWord();
                break;
            case "!#hamdle":
                await StartHamdleSession();
                break;
            default:
                msg = "Invalid command! SirSad";
                break;
        }

        if (!string.IsNullOrEmpty(msg))
        {
            SendMessage?.Invoke(this, msg);
        }
    }

    public async Task StartHamdleSession()
    {
        if (_isHamdleInProgress)
        {
            return;
        }
        _guessTimer = new System.Timers.Timer(120000);
        _guessTimer.Elapsed += OnGuessTimerExpired!;
        SendMessage?.Invoke(this, "Guess a 5 letter word! I will be taking guesses for 2 minutes!");
        _currentWord = await _cache.GetRandomItemFromSet("words")!;
        await _signalRHub.InvokeAsync("SendSelectedWord", _currentWord);
        _isHamdleInProgress = true;
        _guessTimer.Start();
    }

    public bool IsHamdleSessionInProgress()
    {
        return _isHamdleInProgress;
    }

    private void OnGuessTimerExpired(object source, ElapsedEventArgs e)
    {
        _isHamdleInProgress = false;
        SendMessage?.Invoke(this, "The window for guesses is over!");
        var guesses = string.Join(", ", _guesses);
        if (!string.IsNullOrEmpty(guesses))
        {
            SendMessage?.Invoke(this, $"The guesses are: {guesses}. For now I will choose the first one.");
            var selectedGuess = _guesses.First();
            // send to signalr hub for site.
        }
        SendMessage?.Invoke(this, "Nobody guessed! Let's go again.");
        _guessTimer.Elapsed -= OnGuessTimerExpired!;
        Task.Run(async () => await StartHamdleSession()).GetAwaiter().GetResult();
    }
    
    public async Task SubmitGuess(string guess)
    {
        var isValidGuess = await CheckGuess(guess);
        if (isValidGuess)
        {
            _guesses.Add(guess);
        }
    }
    
    private async Task<bool> CheckGuess(string guess)
    {
        var isValidGuess = await _cache.ContainsMember("words", guess) && guess.Length == 5;
        return isValidGuess;
    }
}