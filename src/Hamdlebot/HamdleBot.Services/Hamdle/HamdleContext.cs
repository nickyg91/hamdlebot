using Hamdle.Cache;
using Hamdlebot.Core.Exceptions;
using Hamdlebot.Core.Models.Logging;
using Hamdlebot.Core.SignalR.Clients;
using Hamdlebot.Core.SignalR.Clients.Hamdle;
using Hamdlebot.Core.SignalR.Clients.Logging;
using Hamdlebot.Models.OBS;
using Hamdlebot.Models.OBS.RequestTypes;
using HamdleBot.Services.Hamdle.States;
using HamdleBot.Services.Mediators;

namespace HamdleBot.Services.Hamdle;

public class HamdleContext
{
    private const int StopAndResetDelay = 5000;
    private const int WaitBeforeSceneSet = 1000;
    private readonly ICacheService _cache;
    private readonly IHamdleHubClient _hamdleHubClient;
    private readonly HamdleMediator _mediator;
    private bool _isSceneEnabled;
    private readonly int _hamdleSceneId;
    private readonly IBotLogClient _logClient;
    private BaseState<HamdleContext, IHamdleHubClient>? State { get; set; }
    public event EventHandler<string>? SendMessageToChat;
    public event EventHandler? Restarted;
    public string CurrentWord { get; set; }
    public byte CurrentRound { get; private set; } = 1;
    public bool IsInVotingState => State?.GetType() == typeof(VotingState);
    public bool IsRoundInProgress => State?.GetType() == typeof(GuessState) || IsInVotingState;
    public HashSet<string> Guesses { get; private set; }
    public byte NoGuesses { get; private set; }
    
    public HamdleContext(
        ICacheService cache, 
        IHamdleHubClient hamdleHubClient,
        HamdleMediator mediator,
        int hamdleSceneId,
        IBotLogClient logClient)
    {
        _cache = cache;
        _hamdleHubClient = hamdleHubClient;
        _mediator = mediator;
        CurrentWord = string.Empty;
        Guesses = new HashSet<string>();
        _hamdleSceneId = hamdleSceneId;
        _logClient = logClient;
    }
    
    public void Send(string message)
    {
        SendMessageToChat?.Invoke(this, message);
    }

    public async Task SignalGameFinished()
    {
        Send($"Game over! Nobody has guessed the word. It was {CurrentWord}. Use !#hamdle to begin again.");
        await StopAndReset();
        await Task.Delay(StopAndResetDelay);
        await _hamdleHubClient.ResetState();
    }
    
    public async Task StartGuesses()
    {
        if (!_isSceneEnabled)
        {
            await EnableHamdleScene(true);
        }
        await Task.Delay(WaitBeforeSceneSet);
        var guessState = new GuessState(this, _cache, _hamdleHubClient);
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
        _logClient.LogMessage(new LogMessage($"Hamdle vote submitted by {username} for submission {submission}.", DateTime.UtcNow, SeverityLevel.Info));
        ((VotingState)State).SubmitVoteForGuess(username, submission);
    }
    
    public async Task SubmitGuess(string username, string guess)
    {
        if (State == null || State.GetType() != typeof(GuessState))
        {
            throw new InvalidStateException("State must be GuessState.");
        }
        
        await _logClient.LogMessage(new LogMessage($"Hamdle guess '{guess}' submitted by {username}", DateTime.UtcNow, SeverityLevel.Info));
        await ((GuessState)State).SubmitGuess(username, guess);
    }

    public async Task StopAndReset()
    {
        State = null;
        CurrentWord = "";
        CurrentRound = 1;
        Guesses = [];
        
        await _logClient.LogMessage(new LogMessage($"Stop hamdle context and reset state.", DateTime.UtcNow, SeverityLevel.Info));
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
        
        await _logClient.LogMessage(new LogMessage($"Hamdle voting started.", DateTime.UtcNow, SeverityLevel.Info));
        var votingState = new VotingState(roundGuesses, this, _cache, _hamdleHubClient);
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