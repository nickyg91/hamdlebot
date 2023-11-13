using System.Timers;
using Hamdle.Cache;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services;

public class HamdleWordService : IHamdleWordService
{
    private readonly ICacheService _cache;
    private readonly HubConnection _signalRHub;
    private bool _isHamdleInProgress;
    private bool _isInGuessVotingState;
    private byte _currentChance = 1;
    private byte _maxChances = 5;
    private System.Timers.Timer? _guessTimer;
    private System.Timers.Timer? _voteTimer;
    private HashSet<string> _allGuesses;
    private HashSet<string> _roundGuesses;
    private HashSet<string> _usersWhoGuessed;
    private HashSet<string> _userVotes;
    private string? _currentWord;
    private Dictionary<int, int> _votes;
    private Random _randomNumberGenerator = new ();
    public HamdleWordService(ICacheService cache, HubConnection signalRHub)
    {
        _cache = cache;
        _signalRHub = signalRHub;
        _roundGuesses = new HashSet<string>();
        _allGuesses = new HashSet<string>();
        _votes = new Dictionary<int, int>();
        _userVotes = new HashSet<string>();
        _usersWhoGuessed = new HashSet<string>();
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
                if (!_isHamdleInProgress)
                {
                    await StartHamdleSession();
                }
                break;
        }
    }

    public async Task StartHamdleSession()
    {
        if (_currentChance == 1)
        {
            _currentWord = await _cache.GetRandomItemFromSet("words");
            await _signalRHub.InvokeAsync("SendSelectedWord", _currentWord);
            _isHamdleInProgress = true;
        }
        _guessTimer = new System.Timers.Timer(30000);
        _guessTimer.Elapsed += OnGuessTimerExpired!;
        SendMessage?.Invoke(this, "Guess a 5 letter word!");
        _guessTimer?.Start();
        await _signalRHub.InvokeAsync("StartGuessTimer", 30000);
    }

    public bool IsHamdleSessionInProgress()
    {
        return _isHamdleInProgress;
    }

    public bool IsHamdleVotingInProgress()
    {
        return _isInGuessVotingState;
    }

    private async void OnGuessTimerExpired(object source, ElapsedEventArgs e)
    { 
        if (_currentChance > _maxChances)
        {
            SendMessage?.Invoke(this, $"Nobody has guessed the word. It was {_currentWord}. Use !#hamdle to begin again.");
            await ResetHamdle();
            return;
        }
        SendMessage?.Invoke(this, "The window for guesses is over!");
        _currentChance++;
        if (_roundGuesses.Any())
        {
            await StartVoting();
        }
        else
        {
            SendMessage?.Invoke(this, "Nobody guessed! Let's go again.");
            _currentChance--;
            await StartHamdleSession();
        }
        
        _guessTimer!.Elapsed -= OnGuessTimerExpired!;
    }

    private async Task StartVoting()
    {
        var words = string.Join("\r\n", _roundGuesses.Select((x, idx) => $"{idx + 1}: {x}"));
        Thread.Sleep(1000);
        SendMessage!.Invoke(this, $"Please vote for one the following words:\r\n {words}");
        _voteTimer = new System.Timers.Timer(30000);
        _isInGuessVotingState = true;
        _voteTimer.Elapsed += OnVotingTimerExpired!;
        await _signalRHub.InvokeAsync("StartVoteTimer", 30000);
        _voteTimer.Start();
    }

    private async void OnVotingTimerExpired(object source, ElapsedEventArgs e)
    {
        _isInGuessVotingState = false;

        var key = 0;
        if (!_votes.Keys.Any())
        {
            SendMessage?.Invoke(this, "No one voted. I will select a random guess.");
            key = _randomNumberGenerator.Next(1, _roundGuesses.Count);
        }
        else
        {
            key = _votes.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
        }
        
        var guess = _roundGuesses.ToList().ElementAt(key - 1);
        await _signalRHub.InvokeAsync("SendGuess", guess);
        if (guess == _currentWord)
        {
            SendMessage?.Invoke(this, $"We have a winner! The word was {_currentWord}.");
            SendMessage?.Invoke(this, $"This concludes this instance of hamdle. To initiate another, type !#hamdle!");
            await ResetHamdle();
            return;
        }
        ResetGuessesAndVotes();
        await StartHamdleSession();
        
        _voteTimer!.Elapsed -= OnVotingTimerExpired!;
    }
    
    public async Task SubmitGuess(string username, string guess)
    {
        if (_usersWhoGuessed.Contains(username))
        {
            return;
        }
        
        var isValidGuess = await CheckGuess(guess);
        if (isValidGuess)
        {
            _roundGuesses.Add(guess);
            _allGuesses.Add(guess);
            _usersWhoGuessed.Add(username);
        }
    }

    public void SubmitVoteForGuess(string username, int submission)
    {
        if (_userVotes.Contains(username))
        {
            return;
        }

        _userVotes.Add(username);
        if (_votes.ContainsKey(submission))
        {
            _votes[submission]++;
        }
        else
        {
            _votes.Add(submission, 1);
        }
    }

    private async Task<bool> CheckGuess(string guess)
    {
        var isValidGuess = await _cache.ContainsMember("words", guess) && guess.Length == 5;
        return isValidGuess && !_allGuesses.Contains(guess);
    }

    private void ResetGuessesAndVotes()
    {
        _votes = new Dictionary<int, int>();
        _userVotes = new HashSet<string>();
        _usersWhoGuessed = new HashSet<string>();
        _roundGuesses = new HashSet<string>();
    }

    private async Task ResetHamdle()
    {
        ResetGuessesAndVotes();
        _guessTimer!.Elapsed -= OnGuessTimerExpired!;
        _voteTimer!.Elapsed -= OnVotingTimerExpired!;
        _isHamdleInProgress = false;
        _isInGuessVotingState = false;
        _currentWord = null;
        _currentChance = 1;
        _allGuesses = new HashSet<string>();
        await _signalRHub.InvokeAsync("ResetState");
    }
}