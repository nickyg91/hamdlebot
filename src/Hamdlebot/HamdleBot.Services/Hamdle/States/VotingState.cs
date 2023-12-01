using System.Timers;
using Hamdle.Cache;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services.Hamdle.States;

public class VotingState : BaseState<HamdleContext>
{
    private HashSet<string> _userVotes;
    private Dictionary<int, int> _votes;
    private Random _randomNumberGenerator = new ();
    private readonly HashSet<string> _roundGuesses;
    private System.Timers.Timer? _voteTimer = new (30000)
    {
        AutoReset = false
    };
    
    public VotingState(
        HashSet<string> roundGuesses,
        HamdleContext context, 
        ICacheService cache, 
        HubConnection signalRHub) : base(context, cache, signalRHub)
    {
        _userVotes = new HashSet<string>();
        _votes = new Dictionary<int, int>();
        _voteTimer!.Elapsed += OnVotingTimerExpired!;
        _roundGuesses = roundGuesses;
    }

    public override async Task Start()
    {
        var words = string.Join("\r\n", _roundGuesses.Select((x, idx) => $"{idx + 1}: {x}"));
        Context.Send($"Please vote for one the following words:\r\n {words.ToLower()}");
        await SignalR.InvokeAsync("StartVoteTimer", 30000);
        _voteTimer!.Start();
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
    
    private async void OnVotingTimerExpired(object source, ElapsedEventArgs e)
    {
        if (!_roundGuesses.Any())
        {
            Context.Send("Nobody guessed. Let's guess again.");
            await Context.StartGuesses();
            return;
        }
        var key = 0;
        var allVotesTied = _votes.Values.Distinct().Count() == 1 && _votes.Count > 1;
        if (!_votes.Keys.Any())
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
        await SignalR.InvokeAsync("SendGuess", guess);
        if (guess == Context.CurrentWord)
        {
            Context.Send($"We have a winner! The word was {Context.CurrentWord}.");
            Context.Send($"This concludes this instance of hamdle. To initiate another, type !#hamdle!");
            Thread.Sleep(10000);
            await SignalR.InvokeAsync("ResetState");
            return;
        }

        Context.Guesses.Add(guess);
        Context.CurrentRound++;
        if (Context.CurrentRound > 5)
        {
            await Context.SignalGameFinished();
        }
        else
        {
            Context.Send("10 seconds until next round!");
            await SignalR.InvokeAsync("StartBetweenRoundTimer", 10000);
            Thread.Sleep(10000);
            await Context.StartGuesses();
        }
    }
}