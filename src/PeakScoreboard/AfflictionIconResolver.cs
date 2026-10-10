using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakScoreboard;

/// <summary>
/// Resolves PEAK status icons (Status_Injury, Status_Spores, …) and bar colors.
/// Prefers the HUD status sprites over <see cref="BarAffliction"/> segment glyphs.
/// </summary>
internal static class AfflictionIconResolver
{
    private static readonly Dictionary<CharacterAfflictions.STATUSTYPE, Sprite> Sprites = new();
    private static readonly Dictionary<CharacterAfflictions.STATUSTYPE, Color> Colors = new();
    private static float _nextRetryAt;

    public static void EnsureCache()
    {
        if (Sprites.Count >= 8)
        {
            return;
        }

        if (Time.unscaledTime < _nextRetryAt)
        {
            return;
        }

        _nextRetryAt = Time.unscaledTime + 1f;
        TryCacheFromStatusSprites();
        TryCacheFromStaminaBar();
    }

    public static bool TryGet(CharacterAfflictions.STATUSTYPE type, out Sprite? sprite, out Color color)
    {
        EnsureCache();
        color = GetColor(type);
        if (Sprites.TryGetValue(type, out sprite) && sprite != null)
        {
            return true;
        }

        sprite = null;
        return false;
    }

    public static Color GetColor(CharacterAfflictions.STATUSTYPE type)
    {
        EnsureCache();
        return Colors.TryGetValue(type, out Color cached) ? cached : FallbackColor(type);
    }

    public static string DisplayName(CharacterAfflictions.STATUSTYPE type) => type.ToString();

    private static void TryCacheFromStatusSprites()
    {
        Sprite[] all;
        try
        {
            all = Resources.FindObjectsOfTypeAll<Sprite>();
        }
        catch
        {
            return;
        }

        foreach (CharacterAfflictions.STATUSTYPE type in Enum.GetValues(typeof(CharacterAfflictions.STATUSTYPE)))
        {
            if (Sprites.ContainsKey(type))
            {
                continue;
            }

            Sprite? match = FindStatusSprite(all, type);
            if (match != null)
            {
                Sprites[type] = match;
            }
        }
    }

    private static Sprite? FindStatusSprite(Sprite[] all, CharacterAfflictions.STATUSTYPE type)
    {
        string typeName = type.ToString();
        // Prefer exact PEAK wiki / asset names: Status_Injury, Status Spores, etc.
        string[] exact =
        {
            "Status_" + typeName,
            "Status " + typeName,
            "Status" + typeName,
            typeName + "_Status",
        };

        foreach (string candidate in exact)
        {
            foreach (Sprite sprite in all)
            {
                if (sprite != null && sprite.name.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return sprite;
                }
            }
        }

        // Strict contains: must include both "Status" and the type name as a whole token-ish match.
        foreach (Sprite sprite in all)
        {
            if (sprite == null)
            {
                continue;
            }

            string name = sprite.name;
            if (name.IndexOf("Status", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            if (name.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return sprite;
            }
        }

        return null;
    }

    private static void TryCacheFromStaminaBar()
    {
        GUIManager? gui = GUIManager.instance;
        StaminaBar? bar = gui != null ? gui.bar : null;
        if (bar == null)
        {
            return;
        }

        if (bar.afflictions != null)
        {
            foreach (BarAffliction entry in bar.afflictions)
            {
                if (entry == null || entry.icon == null)
                {
                    continue;
                }

                Colors[entry.afflictionType] = entry.icon.color;
                if (!Sprites.ContainsKey(entry.afflictionType) && entry.icon.sprite != null)
                {
                    Sprites[entry.afflictionType] = entry.icon.sprite;
                }
            }
        }

        if (bar.petrifyAffliction != null && bar.petrifyAffliction.icon != null)
        {
            Colors[CharacterAfflictions.STATUSTYPE.Petrify] = bar.petrifyAffliction.icon.color;
            if (!Sprites.ContainsKey(CharacterAfflictions.STATUSTYPE.Petrify)
                && bar.petrifyAffliction.icon.sprite != null)
            {
                Sprites[CharacterAfflictions.STATUSTYPE.Petrify] = bar.petrifyAffliction.icon.sprite;
            }
        }
    }

    private static Color FallbackColor(CharacterAfflictions.STATUSTYPE type) =>
        type switch
        {
            CharacterAfflictions.STATUSTYPE.Injury => new Color(0.92f, 0.35f, 0.32f, 1f),
            CharacterAfflictions.STATUSTYPE.Hunger => new Color(0.92f, 0.62f, 0.28f, 1f),
            CharacterAfflictions.STATUSTYPE.Cold => new Color(0.45f, 0.75f, 0.95f, 1f),
            CharacterAfflictions.STATUSTYPE.Poison => new Color(0.55f, 0.85f, 0.35f, 1f),
            CharacterAfflictions.STATUSTYPE.Hot => new Color(0.95f, 0.45f, 0.20f, 1f),
            CharacterAfflictions.STATUSTYPE.Drowsy => new Color(0.65f, 0.55f, 0.85f, 1f),
            CharacterAfflictions.STATUSTYPE.Curse => new Color(0.55f, 0.30f, 0.70f, 1f),
            CharacterAfflictions.STATUSTYPE.Thorns => new Color(0.70f, 0.80f, 0.35f, 1f),
            CharacterAfflictions.STATUSTYPE.Spores => new Color(0.75f, 0.55f, 0.90f, 1f),
            CharacterAfflictions.STATUSTYPE.Weight => new Color(0.55f, 0.40f, 0.28f, 1f),
            CharacterAfflictions.STATUSTYPE.Web => new Color(0.85f, 0.85f, 0.80f, 1f),
            CharacterAfflictions.STATUSTYPE.Arrow => new Color(0.90f, 0.75f, 0.40f, 1f),
            CharacterAfflictions.STATUSTYPE.Petrify => new Color(0.55f, 0.55f, 0.60f, 1f),
            CharacterAfflictions.STATUSTYPE.Crab => new Color(0.90f, 0.40f, 0.35f, 1f),
            CharacterAfflictions.STATUSTYPE.FlyTrap => new Color(0.40f, 0.75f, 0.40f, 1f),
            _ => Color.white,
        };
}
