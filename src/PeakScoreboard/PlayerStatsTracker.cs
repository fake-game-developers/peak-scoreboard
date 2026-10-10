using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakScoreboard;

internal readonly struct PlayerRunStats
{
    public PlayerRunStats(float heightMeters, float maxHeightMeters, float distanceMeters, int deaths)
    {
        HeightMeters = heightMeters;
        MaxHeightMeters = maxHeightMeters;
        DistanceMeters = distanceMeters;
        Deaths = deaths;
    }

    public float HeightMeters { get; }
    public float MaxHeightMeters { get; }
    public float DistanceMeters { get; }
    public int Deaths { get; }
}

/// <summary>
/// Per-run, per-scout stats observed locally: height from PEAK's CharacterStats,
/// horizontal distance from position deltas, deaths from dead/ghost edges (+ timeline).
/// </summary>
internal static class PlayerStatsTracker
{
    private const float MinStepUnits = 0.02f;
    private const float MaxStepUnits = 12f;

    private static readonly Dictionary<int, TrackedScout> ByActor = new();
    private static Guid _runId = Guid.Empty;

    public static void Tick()
    {
        EnsureRun();

        foreach (Character character in EnumeratePlayerCharacters())
        {
            int actorId = GetActorId(character);
            if (actorId <= 0)
            {
                continue;
            }

            if (!ByActor.TryGetValue(actorId, out TrackedScout? tracked))
            {
                tracked = new TrackedScout(actorId);
                ByActor[actorId] = tracked;
            }

            tracked.Update(character);
        }
    }

    public static PlayerRunStats GetStats(Character character, int actorId)
    {
        EnsureRun();

        float height = 0f;
        int timelineDeaths = 0;

        CharacterStats? stats = character.refs != null ? character.refs.stats : null;
        if (stats != null)
        {
            height = stats.heightInMeters;
            timelineDeaths = CountTimelineDeaths(stats);
        }

        if (actorId > 0 && ByActor.TryGetValue(actorId, out TrackedScout? tracked))
        {
            return new PlayerRunStats(
                height,
                Mathf.Max(tracked.MaxHeightMeters, height),
                tracked.DistanceMeters,
                Math.Max(tracked.Deaths, timelineDeaths));
        }

        return new PlayerRunStats(height, height, 0f, timelineDeaths);
    }

    private static IEnumerable<Character> EnumeratePlayerCharacters()
    {
        if (PlayerHandler.Exists)
        {
            List<Character> fromHandler = PlayerHandler.GetAllPlayerCharacters();
            if (fromHandler != null && fromHandler.Count > 0)
            {
                foreach (Character character in fromHandler)
                {
                    if (character != null && !character.isBot && !character.isZombie)
                    {
                        yield return character;
                    }
                }

                yield break;
            }
        }

        foreach (Character character in Character.AllCharacters)
        {
            if (character != null && !character.isBot && !character.isZombie)
            {
                yield return character;
            }
        }
    }

    private static int GetActorId(Character character)
    {
        try
        {
            return character.photonView?.OwnerActorNr ?? -1;
        }
        catch
        {
            return -1;
        }
    }

    private static void EnsureRun()
    {
        Guid current = Guid.Empty;
        try
        {
            if (RunManager.Instance != null)
            {
                current = RunManager.Instance.RunId;
            }
        }
        catch
        {
            // RunManager may be unavailable in menus.
        }

        if (current == Guid.Empty)
        {
            return;
        }

        if (_runId != current)
        {
            _runId = current;
            ByActor.Clear();
        }
    }

    private static int CountTimelineDeaths(CharacterStats stats)
    {
        if (stats.timelineInfo == null || stats.timelineInfo.Count == 0)
        {
            return 0;
        }

        int count = 0;
        foreach (EndScreen.TimelineInfo entry in stats.timelineInfo)
        {
            if (entry.died)
            {
                count++;
            }
        }

        return count;
    }

    private sealed class TrackedScout
    {
        private Vector3? _lastPosition;
        private bool _wasDead;
        private bool _initialized;

        public TrackedScout(int actorId) => ActorId = actorId;

        public int ActorId { get; }
        public float DistanceMeters { get; private set; }
        public float MaxHeightMeters { get; private set; }
        public int Deaths { get; private set; }

        public void Update(Character character)
        {
            bool dead = character.data != null && (character.data.dead || character.IsGhost);
            if (_initialized && !_wasDead && dead)
            {
                Deaths++;
            }

            _wasDead = dead;
            _initialized = true;

            CharacterStats? stats = character.refs != null ? character.refs.stats : null;
            if (stats != null)
            {
                MaxHeightMeters = Mathf.Max(MaxHeightMeters, stats.heightInMeters);
            }

            if (dead || character.warping)
            {
                _lastPosition = null;
                return;
            }

            Vector3 position = character.Center;
            if (!_lastPosition.HasValue)
            {
                _lastPosition = position;
                return;
            }

            Vector3 delta = position - _lastPosition.Value;
            delta.y = 0f;
            float step = delta.magnitude;
            _lastPosition = position;

            if (step < MinStepUnits || step > MaxStepUnits)
            {
                return;
            }

            DistanceMeters += step * CharacterStats.unitsToMeters;
        }
    }
}
