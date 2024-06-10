using Hamdlebot.Core;

namespace HamdleBot.Services.Handlers;

public class TwitchAuthTokenUpdateHandler : IObservable<string>
{
    private readonly HashSet<IObserver<string>> _observers = [];
    private string? _botAccessToken;
    public IDisposable Subscribe(IObserver<string> observer)
    {
        if (_observers.Add(observer))
        {
            foreach (var item in _observers)
            {
                if (_botAccessToken != null)
                {
                    item.OnNext(_botAccessToken);    
                }
            }
        }
        return new Unsubscriber<string>(_observers, observer);
    }
    
    public void UpdateToken(string botAccessToken)
    {
        _botAccessToken = botAccessToken;
        foreach (var observer in _observers)
        {
            observer.OnNext(botAccessToken);
        }
    }
}