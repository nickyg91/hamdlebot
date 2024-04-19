using System.Timers;
using Hamdle.Cache;
using Hamdlebot.Core.SignalR.Clients;

namespace HamdleBot.Services.Hamdle.States;

public class GuessState : BaseState<HamdleContext, IHamdleHubClient>
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
    public GuessState(HamdleContext context, ICacheService cache, IHamdleHubClient hamdleHubClient) 
        : base(context, cache, hamdleHubClient)
    {
        _guessTimer!.Elapsed += OnGuessTimerExpired!;
        _guesses = new HashSet<string>();
        _usersWhoGuessed = new HashSet<string>();
    }
    
    public override async Task Start()
    {
        if (Context.CurrentRound == 1)
        {
            var word = await Cache.GetRandomItemFromSet("words");
            if (string.IsNullOrEmpty(word))
            {
                throw new NullReferenceException("No word to set.");
            }

            Context.CurrentWord = word;
            await HubClient!.SendSelectedWord(Context.CurrentWord);
        }
        Context.Send("Guess a 5 letter word!");
        await HubClient!.StartGuessTimer(GuessTimer);
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

    private async Task CorrectWordGuessed()
    {
        Context.Send($"We have a winner! The word was {Context.CurrentWord}.");
        Context.Send($"This concludes this instance of hamdle. To initiate another, type !#hamdle!");
        await Task.Delay(TimeBetweenRounds);
        await Context.StopAndReset();
    }
    
    private async void OnGuessTimerExpired(object source, ElapsedEventArgs e)
    {
        Context.Send("The window for guesses is over!");
        if (_guesses.Count == 1)
        {
            var guess = _guesses.First();
            if (guess == Context.CurrentWord)
            {
                await HubClient!.SendGuess(guess);
                await CorrectWordGuessed();
                return;
            }
            Context.Send("Only one guess was submitted. Let's take that one.");
            Context.IncrementCurrentRound();
            await HubClient!.SendGuess(guess);
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
                await Context.StopAndReset();
            }
            else
            {
                await Context.StartGuesses();
            }
        }
    }
}