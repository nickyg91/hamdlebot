using System.Timers;
using Microsoft.AspNetCore.SignalR.Client;

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
    
    public VotingState(
        HashSet<string> roundGuesses,
        HamdleContext context) : base(context)
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
        await Context.HubConnection.InvokeAsync("StartVoteTimer", VoteTimerDuration, Context.TwitchUserId.ToString());
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
        await Context.HubConnection.InvokeAsync("SendGuess", guess, Context.TwitchUserId.ToString());
        if (guess == Context.CurrentWord)
        {
            Context.Send($"We have a winner! The word was {Context.CurrentWord}.");
            Context.Send($"This concludes this instance of hamdle. To initiate another, type !#hamdle!");
            await Task.Delay(BetweenRoundTimerDuration);
            await Context.HubConnection.SendAsync("SendResetState", Context.TwitchUserId.ToString());
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
            await Context.HubConnection.InvokeAsync("StartBetweenRoundTimer", BetweenRoundTimerDuration, Context.TwitchUserId.ToString());
            await Task.Delay(BetweenRoundTimerDuration);
            await Context.StartGuesses();
        }
    }
}