using System.Timers;
using Hamdle.Cache;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services.Hamdle.States;

public class GuessState : BaseState<HamdleContext>
{
    public event EventHandler<HashSet<string>>? StartVoting;
    
    private readonly HashSet<string> _guesses;
    private HashSet<string> _usersWhoGuessed;
    private System.Timers.Timer? _guessTimer = new (30000)
    {
        AutoReset = false,
    };
    
    public GuessState(HamdleContext context, ICacheService cache, HubConnection signalRHub) 
        : base(context, cache, signalRHub)
    {
        _guessTimer!.Elapsed += OnGuessTimerExpired!;
        _guesses = new HashSet<string>();
        _usersWhoGuessed = new HashSet<string>();
    }
//TODO figure out random guess issue
    public override async Task Start()
    {
        if (Context!.CurrentRound == 1)
        {
            var word = await Cache.GetRandomItemFromSet("words");
            if (string.IsNullOrEmpty(word))
            {
                throw new NullReferenceException("No word to set.");
            }

            Context.CurrentWord = word;
            await SignalR.InvokeAsync("SendSelectedWord", Context.CurrentWord);
        }
        Context.Send("Guess a 5 letter word!");
        await SignalR.InvokeAsync("StartGuessTimer", 30000);
        _guessTimer?.Start();
    }
    
    public async Task SubmitGuess(string username, string guess)
    {
        if (_usersWhoGuessed.Contains(username))
        {
            return;
        }
        
        var isValidGuess = await CheckGuess(guess.ToLower());
        if (isValidGuess)
        {
            _guesses.Add(guess.ToLower());
            _usersWhoGuessed.Add(username);
        }
    }
    
    private async Task<bool> CheckGuess(string guess)
    {
        var isValidGuess = await Cache.ContainsMember("words", guess) && guess.Length == 5;
        return isValidGuess && !Context.Guesses.Contains(guess);
    }
    
    private async void OnGuessTimerExpired(object source, ElapsedEventArgs e)
    {
        Context.Send("The window for guesses is over!");
        if (_guesses.Any())
        {
            StartVoting?.Invoke(this, _guesses);
        }
        else
        {
            Context.Send("Nobody guessed! Let's go again.");
            Context.CurrentRound--;
            Context.NoGuesses++;
            if (Context.NoGuesses == 3)
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