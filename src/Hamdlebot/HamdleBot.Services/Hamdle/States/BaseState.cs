using Hamdle.Cache;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services.Hamdle.States;

public abstract class BaseState<T> where T : class
{
    protected T Context { get; private set; }
    protected ICacheService Cache { get; private set; }
    protected HubConnection SignalR { get; private set; }
    protected BaseState(T context, ICacheService cache, HubConnection signalRHub)
    {
        Cache = cache;
        SignalR = signalRHub;
        Context = context;
    }
    public abstract Task Start();
}