using Hamdle.Cache;
using Hamdlebot.Core.SignalR.Clients;
using Microsoft.AspNetCore.SignalR.Client;

namespace HamdleBot.Services.Hamdle.States;

public abstract class BaseState<TType, TSignalRClient> 
    where TType : class
    where TSignalRClient : ISignalrHubClient
{
    protected TType Context { get; private set; }
    protected ICacheService Cache { get; private set; }
    protected TSignalRClient? HubClient { get; private set; }
    protected BaseState(TType context, ICacheService cache, TSignalRClient hubClient)
    {
        Cache = cache;
        HubClient = hubClient;
        Context = context;
    }
    
    protected BaseState(TType context, ICacheService cache)
    {
        Cache = cache;
        Context = context;
    }
    
    public abstract Task Start();
}