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
    private byte _maxChances = 6;
    private System.Timers.Timer? _guessTimer;
    private System.Timers.Timer? _voteTimer;
    private HashSet<string> _guesses;
    private HashSet<string> _usersWhoGuessed;
    private HashSet<string> _userVotes;
    private string? _currentWord;
    private Dictionary<int, int> _votes;
    private Random _randomNumberGenerator = new (); 
    public HamdleWordService(ICacheService cache, HubConnection signalRHub)
    {
        _cache = cache;
        _signalRHub = signalRHub;
        _guesses = new HashSet<string>();
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
            _guessTimer = new System.Timers.Timer(120000);
            _guessTimer.Elapsed += OnGuessTimerExpired!;
            
            _currentWord = await _cache.GetRandomItemFromSet("words");
            await _signalRHub.InvokeAsync("SendSelectedWord", _currentWord);
            _isHamdleInProgress = true;
        }
        SendMessage?.Invoke(this, "Guess a 5 letter word! I will be taking guesses for 2 minutes!");
        _guessTimer?.Start();
    }

    public bool IsHamdleSessionInProgress()
    {
        return _isHamdleInProgress;
    }

    public bool IsHamdleVotingInProgress()
    {
        return _isInGuessVotingState;
    }

    private void OnGuessTimerExpired(object source, ElapsedEventArgs e)
    { 
        if (_currentChance > _maxChances)
        {
            SendMessage?.Invoke(this, $"Nobody has guessed the word. It was {_currentWord}. Use !#hamdle to begin again.");
            _guessTimer!.Elapsed -= OnGuessTimerExpired!;
            _currentChance = 1;
            _guessTimer.Dispose();
            return;
        }
        _currentChance++;
        _isHamdleInProgress = false;
        SendMessage?.Invoke(this, "The window for guesses is over!");
        
        if (_guesses.Any())
        {
            StartVoting();
        }
        else
        {
            SendMessage?.Invoke(this, "Nobody guessed! Let's go again.");
        }
    }

    private void StartVoting()
    {
        var votingTimer = new System.Timers.Timer(60000);
        var words = string.Join("\r\n", _guesses.Select((x, idx) => $"{idx + 1}: {x}"));
        SendMessage?.Invoke(this, $"Please vote for one the following words:\r\n {words}");
        _isInGuessVotingState = true;
        votingTimer.Elapsed += OnVotingTimerExpired!;
        votingTimer.Start();
    }

    private void OnVotingTimerExpired(object source, ElapsedEventArgs e)
    {
        _isInGuessVotingState = true;

        var key = 0;
        if (!_votes.Keys.Any())
        {
            SendMessage?.Invoke(this, "No one voted. I will select a random guess.");
            var maxKey = _votes.Keys.Max();
            key = _randomNumberGenerator.Next(1, maxKey);
        }
        else
        {
            key = _votes.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
        }
        
        var guess = _guesses.ToList().ElementAt(key);
        Task.Run(async () => await _signalRHub.InvokeAsync("SendGuess", guess));
        if (guess == _currentWord)
        {
            SendMessage?.Invoke(this, $"We have a winner! The word was {_currentWord}.");
            SendMessage?.Invoke(this, $"This concludes this instance of hamdle. To initiate another, type !#hamdle!");
        }
        else
        {
            ResetGuessesAndVotes();
            Task.Run(async () => await StartHamdleSession()).GetAwaiter().GetResult();
        }
        _guessTimer!.Elapsed -= OnGuessTimerExpired!;
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
            _guesses.Add(guess);
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
        return isValidGuess;
    }

    private void ResetGuessesAndVotes()
    {
        _votes = new Dictionary<int, int>();
        _userVotes = new HashSet<string>();
        _usersWhoGuessed = new HashSet<string>();
    }
}