using System.Timers;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services.Hamdle.States;

public class GuessState : BaseState<HamdleContext>
{
    private const int GuessTimer = 30000;
    private const int TimeBetweenRounds = 10000;
    private const int MaxNoGuesses = 3;
    private readonly HashSet<string> _guesses;
    private readonly HashSet<string> _usersWhoGuessed;
    private readonly System.Timers.Timer? _guessTimer = new (GuessTimer)
    {
        AutoReset = false,
    };
    public event EventHandler<HashSet<string>>? StartVoting;
    public event EventHandler? Reset;
    
    public GuessState(
        HamdleContext context) 
        : base(context)
    {
        _guessTimer!.Elapsed += OnGuessTimerExpired!;
        _guesses = new HashSet<string>();
        _usersWhoGuessed = new HashSet<string>();
    }
    
    public override async Task Start()
    {
        if (Context.CurrentRound == 1)
        {
            Reset?.Invoke(this, EventArgs.Empty);
            if (string.IsNullOrEmpty(Context.CurrentWord))
            {
                throw new NullReferenceException("No word to set.");
            }
            await Context.HubConnection.InvokeAsync("SendSelectedWord", Context.CurrentWord, Context.TwitchUserId.ToString());
        }
        Context.Send("Guess a 5 letter word!");
        await Context.HubConnection.InvokeAsync("StartGuessTimer", GuessTimer, Context.TwitchUserId.ToString());
        _guessTimer?.Start();
    }
    
    public void SubmitGuess(string username, string guess)
    {
        if (_usersWhoGuessed.Contains(username))
        {
            return;
        }
        var isValidGuess = CheckGuess(guess.ToLower());
        if (!isValidGuess)
        {
            return;
        }
        _guesses.Add(guess.ToLower());
        _usersWhoGuessed.Add(username);
    }
    
    private bool CheckGuess(string guess)
    {
        var isValidGuess = Context.Words.Contains(guess) && guess.Length == 5;
        return isValidGuess && !Context.Guesses.Contains(guess);
    }

    private async Task CorrectWordGuessed()
    {
        Context.Send($"We have a winner! The word was {Context.CurrentWord}.");
        Context.Send($"This concludes this instance of hamdle. To initiate another, type !#hamdle!");
        await Task.Delay(TimeBetweenRounds);
        Context.StopAndReset();
    }
    
    private async void OnGuessTimerExpired(object source, ElapsedEventArgs e)
    {
        Context.Send("The window for guesses is over!");
        if (_guesses.Count == 1)
        {
            var guess = _guesses.First();
            await Context.HubConnection.InvokeAsync("SendGuess", guess, Context.TwitchUserId.ToString());
            if (guess == Context.CurrentWord)
            {
                await CorrectWordGuessed();
                return;
            }
            Context.Send("Only one guess was submitted. Let's take that one.");
            Context.IncrementCurrentRound();
            if (Context.CurrentRound > 5)
            {
                
                await Context.SignalGameFinished();
                return;
            }
            await Context.StartGuesses();
        }
        else if (_guesses.Count > 1)
        {
            StartVoting?.Invoke(this, _guesses);
        }
        else
        {
            Context.Send("Nobody guessed! Let's go again.");
            Context.DecrementCurrentRound();
            Context.IncrementNoGuesses();
            if (Context.NoGuesses == MaxNoGuesses)
            {
                _guessTimer!.Stop();
                Context.Send("Nobody is playing SirSad. Stopping the game.");
                Context.StopAndReset();
            }
            else
            {
                await Context.StartGuesses();
            }
        }
    }
}