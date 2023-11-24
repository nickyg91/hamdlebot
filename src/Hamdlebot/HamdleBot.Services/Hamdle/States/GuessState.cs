using System.Timers;
using Hamdle.Cache;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services.Hamdle.States;

public class GuessState : BaseState<HamdleContext>
{
    public event EventHandler<HashSet<string>> StartVoting;
    
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
        Context.SendMessage("Guess a 5 letter word!");
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
        Context!.CurrentRound++;
        if (Context.CurrentRound > Context.MaxRounds)
        {
            Context.SendMessage($"Nobody has guessed the word. It was {Context.CurrentWord}. Use !#hamdle to begin again.");
            Context.StopAndReset();
            return;
        }
        Context.SendMessage("The window for guesses is over!");
        if (_guesses.Any())
        {
            StartVoting.Invoke(this, _guesses);
        }
        else
        {
            Context.SendMessage("Nobody guessed! Let's go again.");
            Context.CurrentRound--;
            Context.NoGuesses++;
            if (Context.NoGuesses == 3)
            {
                _guessTimer!.Stop();
                Context.SendMessage("Nobody is playing SirSad. Stopping the game.");
                Context.StopAndReset();
            }
            else
            {
                await Context.StartGuesses();
            }
        }
    }
}