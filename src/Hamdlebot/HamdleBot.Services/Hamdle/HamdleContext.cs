using Hamdlebot.Core.Exceptions;
using Hamdlebot.Core.SignalR.Clients.Hamdle;
using HamdleBot.Services.Hamdle.States;

namespace HamdleBot.Services.Hamdle;

public class HamdleContext
{
    private const int StopAndResetDelay = 5000;
    private const int WaitBeforeSceneSet = 1000;
    private readonly long _twitchUserId;
    public HashSet<string> Words { get; init; }
    private BaseState<HamdleContext>? State { get; set; }
    public event EventHandler<string>? SendMessageToChat;
    public event EventHandler? Restarted;
    
    public string CurrentWord { get; init; }
    public byte CurrentRound { get; private set; } = 1;
    public bool IsInVotingState => State?.GetType() == typeof(VotingState);
    public bool IsRoundInProgress => State?.GetType() == typeof(GuessState) || IsInVotingState;
    public HashSet<string> Guesses { get; private set; }
    public byte NoGuesses { get; private set; }
    
    public HamdleContext(
        HashSet<string> words,
        string currentWord,
        long twitchUserId)
    {
        Words = words;
        CurrentWord = currentWord;
        Guesses = new HashSet<string>();
        _twitchUserId = twitchUserId;
    }
    
    public void Send(string message)
    {
        SendMessageToChat?.Invoke(this, message);
    }

    public async Task SignalGameFinished()
    {
        Send($"Game over! Nobody has guessed the word. It was {CurrentWord}. Use !#hamdle to begin again.");
        StopAndReset();
        await Task.Delay(StopAndResetDelay);
        //await _hamdleHubClient.ResetState();
    }
    
    public async Task StartGuesses()
    {
        await Task.Delay(WaitBeforeSceneSet);
        var guessState = new GuessState(this);
        guessState.StartVoting += StartVoting!;
        State = guessState;
        await State.Start();
    }

    public void SubmitVoteForGuess(string username, int submission)
    {
        if (State == null || State.GetType() != typeof(VotingState))
        {
            throw new InvalidStateException("State must be VotingState.");
        }
        ((VotingState)State).SubmitVoteForGuess(username, submission);
    }
    
    public void SubmitGuess(string username, string guess)
    {
        if (State == null || State.GetType() != typeof(GuessState))
        {
            throw new InvalidStateException("State must be GuessState.");
        }
        ((GuessState)State).SubmitGuess(username, guess);
    }

    public void StopAndReset()
    {   
        Restarted?.Invoke(this, null!);
    }

    public void DecrementCurrentRound()
    {
        CurrentRound--;
    }

    public void IncrementCurrentRound()
    {
        CurrentRound++;
    }

    public void IncrementNoGuesses()
    {
        NoGuesses++;
    }
    
    private async void StartVoting(object sender, HashSet<string> roundGuesses)
    {
        if (State == null || State.GetType() != typeof(GuessState))
        {
            throw new InvalidStateException("State must be GuessState.");
        }
        var votingState = new VotingState(roundGuesses, this);
        State = votingState;
        await State.Start();
    }
}