namespace ZombieShooter.Core;

// Observer: the session is the subject; hosts and the facade's event log subscribe at runtime.
public interface IGameObserver
{
    void OnGameEvent(GameEvent message);
}

public interface IGameSubject
{
    void Attach(IGameObserver observer);
    void Detach(IGameObserver observer);
    void Notify(GameEvent message);
}
