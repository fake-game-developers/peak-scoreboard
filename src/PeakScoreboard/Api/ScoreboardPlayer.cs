namespace PeakScoreboard.Api;

/// <summary>
/// Snapshot of a scout as shown on the scoreboard. Passed to column value getters and row actions.
/// </summary>
public readonly struct ScoreboardPlayer
{
    public ScoreboardPlayer(
        Character character,
        int actorId,
        string name,
        bool isLocal,
        bool isDead,
        bool isPassedOut,
        string statusLabel,
        float heightMeters,
        float distanceMeters,
        int deaths)
    {
        Character = character;
        ActorId = actorId;
        Name = name ?? string.Empty;
        IsLocal = isLocal;
        IsDead = isDead;
        IsPassedOut = isPassedOut;
        StatusLabel = statusLabel ?? string.Empty;
        HeightMeters = heightMeters;
        DistanceMeters = distanceMeters;
        Deaths = deaths;
    }

    /// <summary>Live PEAK <see cref="Character"/> for this row. May be destroyed after the callback returns.</summary>
    public Character Character { get; }

    /// <summary>Photon actor number, or -1 when unknown.</summary>
    public int ActorId { get; }

    public string Name { get; }
    public bool IsLocal { get; }
    public bool IsDead { get; }
    public bool IsPassedOut { get; }

    /// <summary>Alive / Down / Dead.</summary>
    public string StatusLabel { get; }

    public float HeightMeters { get; }
    public float DistanceMeters { get; }
    public int Deaths { get; }
}
