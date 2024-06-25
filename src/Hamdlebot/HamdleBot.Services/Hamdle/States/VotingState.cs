using System.Timers;
using Hamdle.Cache;
using Hamdlebot.Core.SignalR.Clients;
using Hamdlebot.Core.SignalR.Clients.Hamdle;

namespace HamdleBot.Services.Hamdle.States;

public class VotingState : BaseState<HamdleContext>
{
    private readonly HashSet<string> _userVotes;
    private readonly Dictionary<int, int> _votes;
    private readonly Random _randomNumberGenerator = new ();
    private readonly HashSet<string> _roundGuesses;
    private const int VoteTimerDuration = 30000;
    private const int BetweenRoundTimerDuration = 10000;
    private readonly System.Timers.Timer? _voteTimer = new (VoteTimerDuration)
    {
        AutoReset = false
    };

    public event EventHandler<int>? StartBetweenRoundTimer;
    public event EventHandler<int>? StartVoteTimer;
    public event EventHandler<string>? SendGuess;
    public event EventHandler? ResetState; 
    
    public VotingState(
        HashSet<string> roundGuesses,
        HamdleContext context) : base(context)
    {
        _userVotes = new HashSet<string>();
        _votes = new Dictionary<int, int>();
        _voteTimer!.Elapsed += OnVotingTimerExpired!;
        _roundGuesses = roundGuesses;
    }

    public override Task Start()
    {
        var words = string.Join("\r\n", _roundGuesses.Select((x, idx) => $"{idx + 1}: {x}"));
        Context.Send($"Please vote for one the following words:\r\n {words.ToLower()}");
        StartVoteTimer?.Invoke(this, VoteTimerDuration);
        _voteTimer!.Start();
        return Task.CompletedTask;
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
        if (!_votes.TryAdd(submission, 1))
        {
            _votes[submission]++;
        }
    }
    
    private async void OnVotingTimerExpired(object source, ElapsedEventArgs e)
    {
        if (_roundGuesses.Count == 0)
        {
            Context.Send("Nobody guessed. Let's guess again.");
            await Context.StartGuesses();
            return;
        }
        int key;
        var allVotesTied = _votes.Values.Distinct().Count() == 1 && _votes.Count > 1;
        if (_votes.Keys.Count == 0)
        {
            Context.Send("No one voted. I will select a random guess.");
            key = _randomNumberGenerator.Next(1, _roundGuesses.Count);
        }
        else if (allVotesTied)
        {
            Context.Send("Votes are tied between all guesses! Taking a random word for fairness.");
            key = _randomNumberGenerator.Next(1, _roundGuesses.Count + 1);
        }
        else
        {
            key = _votes.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
        }
        
        var guess = _roundGuesses.ToList().ElementAt(key - 1);
        SendGuess?.Invoke(this, guess);
        if (guess == Context.CurrentWord)
        {
            Context.Send($"We have a winner! The word was {Context.CurrentWord}.");
            Context.Send($"This concludes this instance of hamdle. To initiate another, type !#hamdle!");
            await Task.Delay(BetweenRoundTimerDuration);
            ResetState?.Invoke(this, EventArgs.Empty);
            return;
        }

        Context.Guesses.Add(guess);
        Context.IncrementCurrentRound();
        if (Context.CurrentRound > 5)
        {
            await Context.SignalGameFinished();
        }
        else
        {
            Context.Send("10 seconds until next round!");
            StartBetweenRoundTimer?.Invoke(this, BetweenRoundTimerDuration);
            await Task.Delay(BetweenRoundTimerDuration);
            await Context.StartGuesses();
        }
    }
}