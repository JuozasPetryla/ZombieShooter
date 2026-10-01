// Singleton
using System;

namespace ZombieShooter;

public sealed class HighScores
{
    private static readonly Lazy<HighScores> _instance = new(() => new HighScores());
    public static HighScores Instance => _instance.Value;

    private HighScores() { }

    public int Best { get; private set; }
    public void Submit(int score) { if (score > Best) Best = score; }
}
