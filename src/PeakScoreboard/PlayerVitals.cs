using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace PeakScoreboard;

internal readonly struct PlayerEffect
{
    public PlayerEffect(CharacterAfflictions.STATUSTYPE type, float amount)
    {
        Type = type;
        Amount = amount;
    }

    public CharacterAfflictions.STATUSTYPE Type { get; }
    public float Amount { get; }

    /// <summary>PEAK displays status as points on a 100-wide bar.</summary>
    public int Points => Mathf.RoundToInt(Amount * 100f);
}

internal readonly struct PlayerVitals
{
    // Match typical stamina-bar order (weight/hunger first, then damage-like statuses).
    private static readonly CharacterAfflictions.STATUSTYPE[] TrackedStatuses =
    {
        CharacterAfflictions.STATUSTYPE.Weight,
        CharacterAfflictions.STATUSTYPE.Hunger,
        CharacterAfflictions.STATUSTYPE.Injury,
        CharacterAfflictions.STATUSTYPE.Poison,
        CharacterAfflictions.STATUSTYPE.Cold,
        CharacterAfflictions.STATUSTYPE.Hot,
        CharacterAfflictions.STATUSTYPE.Drowsy,
        CharacterAfflictions.STATUSTYPE.Thorns,
        CharacterAfflictions.STATUSTYPE.Arrow,
        CharacterAfflictions.STATUSTYPE.Web,
        CharacterAfflictions.STATUSTYPE.Spores,
        CharacterAfflictions.STATUSTYPE.Curse,
        CharacterAfflictions.STATUSTYPE.Petrify,
        CharacterAfflictions.STATUSTYPE.Crab,
        CharacterAfflictions.STATUSTYPE.FlyTrap,
    };

    public PlayerVitals(
        bool isHost,
        float stamina,
        float extraStamina,
        float maxStamina,
        float availableStamina,
        IReadOnlyList<PlayerEffect> effects)
    {
        IsHost = isHost;
        Stamina = stamina;
        ExtraStamina = extraStamina;
        MaxStamina = maxStamina;
        AvailableStamina = availableStamina;
        Effects = effects;
    }

    public bool IsHost { get; }
    public float Stamina { get; }
    public float ExtraStamina { get; }
    public float MaxStamina { get; }

    /// <summary>
    /// Absolute free stamina on the full 0–1 PEAK bar (currentStamina × GetMaxStamina()).
    /// </summary>
    public float AvailableStamina { get; }

    public IReadOnlyList<PlayerEffect> Effects { get; }

    public float ExtraStaminaFraction => Mathf.Clamp01(ExtraStamina);

    public int AvailablePoints => Mathf.RoundToInt(Mathf.Clamp01(AvailableStamina) * 100f);

    public static PlayerVitals Capture(Character character)
    {
        bool isHost = false;
        try
        {
            Photon.Realtime.Player? owner = character.photonView?.Owner;
            isHost = owner != null && owner.IsMasterClient;
            if (!isHost && character == Character.localCharacter && PhotonNetwork.IsMasterClient)
            {
                isHost = true;
            }
        }
        catch
        {
            // Photon teardown.
        }

        float stamina = 0f;
        float extra = 0f;
        float max = 1f;
        float available = 0f;
        if (character.data != null)
        {
            stamina = Mathf.Clamp01(character.data.currentStamina);
            extra = Mathf.Clamp01(character.data.extraStamina);
            try
            {
                max = Mathf.Max(0f, character.GetMaxStamina());
            }
            catch
            {
                max = 1f;
            }

            available = Mathf.Clamp01(stamina * max);
        }

        return new PlayerVitals(isHost, stamina, extra, max, available, CaptureEffects(character));
    }

    private static IReadOnlyList<PlayerEffect> CaptureEffects(Character character)
    {
        CharacterAfflictions? afflictions = character.refs != null ? character.refs.afflictions : null;
        if (afflictions == null)
        {
            return Array.Empty<PlayerEffect>();
        }

        var active = new List<PlayerEffect>(6);
        foreach (CharacterAfflictions.STATUSTYPE type in TrackedStatuses)
        {
            try
            {
                float amount;
                if (type == CharacterAfflictions.STATUSTYPE.Petrify && character.data != null)
                {
                    amount = Mathf.Clamp01(character.data.petrifyAmount / 100f);
                }
                else
                {
                    amount = afflictions.GetCurrentStatus(type);
                }

                // 1% of the bar (same granularity as HUD numbers).
                if (amount >= 0.0095f)
                {
                    active.Add(new PlayerEffect(type, amount));
                }
            }
            catch
            {
                // Ignore missing status slots.
            }
        }

        return active;
    }
}
