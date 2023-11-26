using Hamdle.Cache;
using Hamdlebot.Core.Exceptions;
using HamdleBot.Services.Hamdle.States;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services.Hamdle;

public class HamdleContext
{
    private readonly ICacheService _cache;
    private readonly HubConnection _signalRHub;
    private event EventHandler<string>? SendMessage;
    public HamdleContext(
        ICacheService cache, 
        HubConnection signalRHub, EventHandler<string>? sendMessage)
    {
        _cache = cache;
        _signalRHub = signalRHub;
        CurrentWord = string.Empty;
        Guesses = new HashSet<string>();
        SendMessage = sendMessage;
    }
    public string CurrentWord { get; set; }
    public byte CurrentRound { get; set; } = 1;
    private BaseState<HamdleContext>? State { get; set; }
    public bool IsInVotingState => State?.GetType() == typeof(VotingState);
    public bool IsRoundInProgress => State?.GetType() == typeof(GuessState) || IsInVotingState;
    public HashSet<string> Guesses { get; set; }
    public byte NoGuesses { get; set; }
    
    public void Send(string message)
    {
        SendMessage!.Invoke(this, message);
    }

    public async Task SignalGameFinished()
    {
        Console.WriteLine(CurrentRound);
        Send($"Game over! Nobody has guessed the word. It was {CurrentWord}. Use !#hamdle to begin again.");
        StopAndReset();
        await _signalRHub.InvokeAsync("ResetState");
    }
    
    public async Task StartGuesses()
    {
        var guessState = new GuessState(this, _cache, _signalRHub);
        guessState.StartVoting += StartVoting!;
        State = guessState;
        await State.Start();
    }

    private async void StartVoting(object sender, HashSet<string> roundGuesses)
    {
        if (State == null || State.GetType() != typeof(GuessState))
        {
            throw new InvalidStateException("State must be GuessState.");
        }
        var votingState = new VotingState(roundGuesses, this, _cache, _signalRHub);
        State = votingState;
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
    
    public async Task SubmitGuess(string username, string guess)
    {
        if (State == null || State.GetType() != typeof(GuessState))
        {
            throw new InvalidStateException("State must be GuessState.");
        }
        await ((GuessState)State).SubmitGuess(username, guess);
    }

    public void StopAndReset()
    {
        State = null;
        CurrentWord = "";
        CurrentRound = 1;
        Guesses = new HashSet<string>();
    }
}