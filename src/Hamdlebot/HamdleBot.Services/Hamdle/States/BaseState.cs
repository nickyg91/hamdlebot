namespace HamdleBot.Services.Hamdle.States;

public abstract class BaseState<TType> 
    where TType : class
{
    protected TType Context { get; private set; }
    protected BaseState(TType context)
    {
        Context = context;
    }
    
    public abstract Task Start();
}