using System.Timers;
using Hamdle.Cache;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services;

public class HamdleWordService : IHamdleWordService
{
    private readonly ICacheService _cache;
    private readonly HubConnection _signalRHub;
    private bool _isHamdleRoundInProgress;
    private bool _isInGuessVotingState;
    private byte _currentRound = 1;
    private byte _maxRound = 5;
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
    public event EventHandler<OBSRequest<GetSceneItemListRequest>>? SendGetSceneItemListRequestToObs;

    //TODO
    //BUGS
    //Between round timer not showing. 
    //Need to select random item if all votes are tied.
    //Make sure everything is lowercase.
    //Do not add item to all guesses if word not chosen after voting. 
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
                if (!_isHamdleRoundInProgress)
                {
                    await StartHamdleRound();
                }
                break;
        }
    }

    public async Task StartHamdleRound()
    {
        _isInGuessVotingState = false;
        _isHamdleRoundInProgress = true;
        if (_currentRound == 1)
        {
            _currentWord = await _cache.GetRandomItemFromSet("words");
            await _signalRHub.InvokeAsync("SendSelectedWord", _currentWord);
        }
        _guessTimer = new System.Timers.Timer(30000);
        _guessTimer.Elapsed += OnGuessTimerExpired!;
        SendMessage?.Invoke(this, "Guess a 5 letter word!");
        await _signalRHub.InvokeAsync("StartGuessTimer", 30000);
        _guessTimer?.Start();
    }

    public bool IsHamdleSessionInProgress()
    {
        return _isHamdleRoundInProgress;
    }

    public bool IsHamdleVotingInProgress()
    {
        return _isInGuessVotingState;
    }

    private async void OnGuessTimerExpired(object source, ElapsedEventArgs e)
    {
        _guessTimer!.Elapsed -= OnGuessTimerExpired!;
        _currentRound++;
        if (_currentRound > _maxRound)
        {
            SendMessage?.Invoke(this, $"Nobody has guessed the word. It was {_currentWord}. Use !#hamdle to begin again.");
            await ResetHamdle();
            return;
        }
        SendMessage?.Invoke(this, "The window for guesses is over!");
        if (_roundGuesses.Any())
        {
            await StartVoting();
        }
        else
        {
            SendMessage?.Invoke(this, "Nobody guessed! Let's go again.");
            _currentRound--;
            await StartHamdleRound();
        }
    }

    private async Task StartVoting()
    {
        _isInGuessVotingState = true;
        var words = string.Join("\r\n", _roundGuesses.Select((x, idx) => $"{idx + 1}: {x}"));
        SendMessage!.Invoke(this, $"Please vote for one the following words:\r\n {words.ToLower()}");
        _voteTimer = new System.Timers.Timer(30000);
        _voteTimer.Elapsed += OnVotingTimerExpired!;
        await _signalRHub.InvokeAsync("StartVoteTimer", 30000);
        _voteTimer.Start();
    }

    private async void OnVotingTimerExpired(object source, ElapsedEventArgs e)
    {
        _isInGuessVotingState = false;
        if (!_roundGuesses.Any())
        {
            SendMessage?.Invoke(this, "Nobody guessed. Let's guess again.");
            _voteTimer!.Elapsed -= OnVotingTimerExpired!;
            await StartHamdleRound();
            return;
        }
        
        var key = 0;
        //add a case for tied votes
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
            Thread.Sleep(10000);
            _voteTimer!.Elapsed -= OnVotingTimerExpired!;
            await ResetHamdle();
            return;
        }
        ResetGuessesAndVotes();
        SendMessage?.Invoke(this, $"10 seconds until next round!");
        await _signalRHub.InvokeAsync("StartBetweenRoundTimer", 10000);
        Thread.Sleep(5000);
        _voteTimer!.Elapsed -= OnVotingTimerExpired!;
        await StartHamdleRound();
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

        if (submission > _roundGuesses.Count)
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
        _isHamdleRoundInProgress = false;
        _isInGuessVotingState = false;
        _currentWord = null;
        _currentRound = 1;
        _allGuesses = new HashSet<string>();
        await _signalRHub.InvokeAsync("ResetState");
    }
}