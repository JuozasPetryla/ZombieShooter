using System.Threading;

namespace ZombieShooter;

// Lazy<T> protects construction; CAS protects concurrent maximum updates.
public sealed class HighScores
{
    private static readonly Lazy<HighScores> InstanceHolder = new(() => new HighScores());
    public static HighScores Instance => InstanceHolder.Value;
    private int _best;
    private HighScores() { }
    public int Best => Volatile.Read(ref _best);
    public void Submit(int score)
    {
        int observed;
        do
        {
            observed = Best;
            if (score <= observed) return;
        } while (Interlocked.CompareExchange(ref _best, score, observed) != observed);
    }
}
