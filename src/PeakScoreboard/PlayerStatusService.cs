using System;
using System.Collections.Generic;
using System.Linq;

namespace PeakScoreboard;

internal readonly struct PlayerStatusRow
{
    public PlayerStatusRow(
        Character character,
        int actorId,
        string name,
        bool isLocal,
        bool isDead,
        bool isPassedOut,
        PlayerRunStats stats,
        PlayerVitals vitals)
    {
        Character = character;
        ActorId = actorId;
        Name = name;
        IsLocal = isLocal;
        IsDead = isDead;
        IsPassedOut = isPassedOut;
        Stats = stats;
        Vitals = vitals;
    }

    public Character Character { get; }
    public int ActorId { get; }
    public string Name { get; }
    public bool IsLocal { get; }
    public bool IsDead { get; }
    public bool IsPassedOut { get; }
    public PlayerRunStats Stats { get; }
    public PlayerVitals Vitals { get; }

    public string StatusLabel
    {
        get
        {
            if (IsDead)
            {
                return "Dead";
            }

            if (IsPassedOut)
            {
                return "Down";
            }

            return "Alive";
        }
    }

    public bool CanRespawn => IsDead || Character.IsGhost;

    /// <summary>Alive = 0, Down = 1, Dead = 2 — lower ranks first on the board.</summary>
    public int StatusRank => IsDead ? 2 : IsPassedOut ? 1 : 0;
}

internal static class PlayerStatusService
{
    public static IReadOnlyList<PlayerStatusRow> Capture()
    {
        Character[] characters = GetPlayerCharacters();
        var rows = new List<PlayerStatusRow>(characters.Length);

        foreach (Character character in characters)
        {
            int actorId = GetActorId(character);
            string name = ResolveName(character);
            bool isDead = character.data != null && (character.data.dead || character.IsGhost);
            bool isPassedOut = !isDead
                && character.data != null
                && (character.data.passedOut || character.data.fullyPassedOut);
            PlayerRunStats stats = PlayerStatsTracker.GetStats(character, actorId);
            PlayerVitals vitals = PlayerVitals.Capture(character);

            rows.Add(new PlayerStatusRow(
                character,
                actorId,
                name,
                character == Character.localCharacter,
                isDead,
                isPassedOut,
                stats,
                vitals));
        }

        // Scoreboard ranking: living scouts first, then by climb / travel / fewer deaths.
        return rows
            .OrderBy(row => row.StatusRank)
            .ThenByDescending(row => row.Stats.MaxHeightMeters)
            .ThenByDescending(row => row.Stats.HeightMeters)
            .ThenByDescending(row => row.Stats.DistanceMeters)
            .ThenBy(row => row.Stats.Deaths)
            .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.ActorId)
            .ToArray();
    }

    private static Character[] GetPlayerCharacters()
    {
        if (!PlayerHandler.Exists)
        {
            return Filter(Character.AllCharacters);
        }

        List<Character> fromHandler = PlayerHandler.GetAllPlayerCharacters();
        if (fromHandler != null && fromHandler.Count > 0)
        {
            return Filter(fromHandler);
        }

        return Filter(Character.AllCharacters);
    }

    private static Character[] Filter(IEnumerable<Character>? source)
    {
        if (source == null)
        {
            return Array.Empty<Character>();
        }

        return source
            .Where(character => character != null && !character.isBot && !character.isZombie)
            .ToArray();
    }

    private static string ResolveName(Character character)
    {
        if (!string.IsNullOrWhiteSpace(character.characterName))
        {
            return character.characterName;
        }

        try
        {
            string? nick = character.photonView?.Owner?.NickName;
            if (!string.IsNullOrWhiteSpace(nick))
            {
                return nick!;
            }
        }
        catch
        {
            // Photon owner can throw during teardown.
        }

        return character.gameObject != null ? character.gameObject.name : "Scout";
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
}
