using Hamdle.Cache;
using Hamdlebot.Core.Exceptions;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using HamdleBot.Services.Hamdle.States;
using HamdleBot.Services.Mediators;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services.Hamdle;

public class HamdleContext
{
    private readonly ICacheService _cache;
    private readonly HubConnection _signalRHub;
    private readonly HamdleMediator _mediator;
    private bool _isSceneEnabled;
    private readonly int _hamdleSceneId;
    public event EventHandler<string>? SendMessageToChat;
    public event EventHandler? Restarted;
    public string CurrentWord { get; set; }
    public byte CurrentRound { get; set; } = 1;
    private BaseState<HamdleContext>? State { get; set; }
    public bool IsInVotingState => State?.GetType() == typeof(VotingState);
    public bool IsRoundInProgress => State?.GetType() == typeof(GuessState) || IsInVotingState;
    public HashSet<string> Guesses { get; set; }
    public byte NoGuesses { get; set; }
    
    public HamdleContext(
        ICacheService cache, 
        HubConnection signalRHub,
        HamdleMediator mediator,
        int hamdleSceneId)
    {
        _cache = cache;
        _signalRHub = signalRHub;
        _mediator = mediator;
        CurrentWord = string.Empty;
        Guesses = new HashSet<string>();
        _hamdleSceneId = hamdleSceneId;
    }
    
    public void Send(string message)
    {
        //await _mediator.SendChatMessage(message);
        SendMessageToChat?.Invoke(this, message);
    }

    public async Task SignalGameFinished()
    {
        Send($"Game over! Nobody has guessed the word. It was {CurrentWord}. Use !#hamdle to begin again.");
        await StopAndReset();
        Thread.Sleep(5000);
        await _signalRHub.InvokeAsync("ResetState");
    }
    
    public async Task StartGuesses()
    {
        if (!_isSceneEnabled)
        {
            await EnableHamdleScene(true);
        }
        Thread.Sleep(1000);
        var guessState = new GuessState(this, _cache, _signalRHub);
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
    
    public async Task SubmitGuess(string username, string guess)
    {
        if (State == null || State.GetType() != typeof(GuessState))
        {
            throw new InvalidStateException("State must be GuessState.");
        }
        await ((GuessState)State).SubmitGuess(username, guess);
    }

    public async Task StopAndReset()
    {
        State = null;
        CurrentWord = "";
        CurrentRound = 1;
        Guesses = new HashSet<string>();
        await EnableHamdleScene(false);
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

    private async Task EnableHamdleScene(bool enabled)
    {
        _isSceneEnabled = enabled;
        await _mediator.SendObsRequest(new ObsRequest<SetSceneItemEnabledRequest>
        {
            Op = OpCodeType.Request,
            RequestData = new RequestWrapper<SetSceneItemEnabledRequest>
            {
                RequestId = Guid.NewGuid(),
                RequestType = ObsRequestStrings.SetSceneItemEnabled,
                RequestData = new SetSceneItemEnabledRequest
                {
                    //make this parameterizable through settings in azure.
                    SceneName = "Desktop Capture",
                    SceneItemEnabled = enabled,
                    SceneItemId = _hamdleSceneId
                }
            }
        });
    }
}