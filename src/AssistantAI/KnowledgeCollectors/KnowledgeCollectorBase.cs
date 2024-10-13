namespace Andronix.AssistantAI.KnowledgeCollectors;

public abstract class KnowledgeCollectorBase
{
    ManualResetEvent _shutdownEvent = new(false);
    Thread _thread;

    public KnowledgeCollectorBase()
    {
        _thread = new Thread(DoWork);
    }

    public void Start()
    {
        _shutdownEvent.Reset();
        _thread.Start();
    }

    public void Stop() 
    {
        if (!_shutdownEvent.WaitOne(0) && _thread.IsAlive)
        {
            _shutdownEvent.Set();
            _thread.Join();
        }
    }

    public abstract void DoWork();

    public bool IsShuttingDown => _shutdownEvent.WaitOne(0);
}
