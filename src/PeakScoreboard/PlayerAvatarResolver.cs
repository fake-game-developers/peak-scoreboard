using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using Zorro.Core;

namespace PeakScoreboard;

internal readonly struct ScoutAvatar
{
    public ScoutAvatar(Color skinColor, Texture? steamAvatar, Texture? eyes, Texture? mouth, Texture? accessory)
    {
        SkinColor = skinColor;
        SteamAvatar = steamAvatar;
        Eyes = eyes;
        Mouth = mouth;
        Accessory = accessory;
    }

    public Color SkinColor { get; }
    public Texture? SteamAvatar { get; }
    public Texture? Eyes { get; }
    public Texture? Mouth { get; }
    public Texture? Accessory { get; }

    public bool HasSteamAvatar => SteamAvatar != null;

    public static ScoutAvatar Fallback { get; } = new(
        new Color(0.55f, 0.58f, 0.56f, 1f),
        null,
        null,
        null,
        null);
}

/// <summary>
/// Resolves a scoreboard avatar: prefers the Steam profile picture when available,
/// otherwise uses PEAK scout skin color with eye / mouth / accessory overlays.
/// </summary>
internal static class PlayerAvatarResolver
{
    private static readonly Dictionary<ulong, Texture2D> SteamAvatarCache = new();
    private static readonly HashSet<ulong> SteamAvatarRequested = new();

    public static ScoutAvatar Resolve(Character character)
    {
        try
        {
            if (character == null)
            {
                return ScoutAvatar.Fallback;
            }

            Color skin = ScoutAvatar.Fallback.SkinColor;
            if (character.refs?.customization != null)
            {
                skin = character.refs.customization.PlayerColor;
            }

            Texture? steam = TryGetSteamAvatar(character);
            CharacterCustomizationData? data = TryGetCustomizationData(character);
            Customization? customization = Singleton<Customization>.Instance;

            Texture? eyes = null;
            Texture? mouth = null;
            Texture? accessory = null;
            if (data != null && customization != null)
            {
                eyes = SafeTexture(customization.eyes, data.currentEyes);
                mouth = SafeTexture(customization.mouths, data.currentMouth);
                accessory = SafeAccessory(customization.accessories, data.currentAccessory);
            }

            return new ScoutAvatar(skin, steam, eyes, mouth, accessory);
        }
        catch
        {
            return ScoutAvatar.Fallback;
        }
    }

    private static Texture? TryGetSteamAvatar(Character character)
    {
        if (!SteamManager.Initialized)
        {
            return null;
        }

        try
        {
            Photon.Realtime.Player? owner = character.photonView?.Owner;
            if (owner == null || string.IsNullOrEmpty(owner.UserId))
            {
                return null;
            }

            if (!ulong.TryParse(owner.UserId, out ulong steamIdValue) || steamIdValue == 0)
            {
                return null;
            }

            if (SteamAvatarCache.TryGetValue(steamIdValue, out Texture2D? cached) && cached != null)
            {
                return cached;
            }

            var steamId = new CSteamID(steamIdValue);
            int imageId = SteamFriends.GetLargeFriendAvatar(steamId);
            if (imageId == -1)
            {
                if (SteamAvatarRequested.Add(steamIdValue))
                {
                    SteamFriends.RequestUserInformation(steamId, bRequireNameOnly: false);
                }

                return null;
            }

            if (imageId == 0)
            {
                return null;
            }

            if (!SteamUtils.GetImageSize(imageId, out uint width, out uint height) || width == 0 || height == 0)
            {
                return null;
            }

            var buffer = new byte[width * height * 4];
            if (!SteamUtils.GetImageRGBA(imageId, buffer, buffer.Length))
            {
                return null;
            }

            // Steam returns top-down RGBA; Unity textures are bottom-up.
            FlipVertically(buffer, (int)width, (int)height);

            var texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, mipChain: false);
            texture.name = $"PeakScoreboard_SteamAvatar_{steamIdValue}";
            texture.LoadRawTextureData(buffer);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            SteamAvatarCache[steamIdValue] = texture;
            return texture;
        }
        catch
        {
            return null;
        }
    }

    private static void FlipVertically(byte[] rgba, int width, int height)
    {
        int stride = width * 4;
        var row = new byte[stride];
        for (int y = 0; y < height / 2; y++)
        {
            int top = y * stride;
            int bottom = (height - 1 - y) * stride;
            Buffer.BlockCopy(rgba, top, row, 0, stride);
            Buffer.BlockCopy(rgba, bottom, rgba, top, stride);
            Buffer.BlockCopy(row, 0, rgba, bottom, stride);
        }
    }

    private static CharacterCustomizationData? TryGetCustomizationData(Character character)
    {
        try
        {
            Photon.Realtime.Player? owner = character.photonView?.Owner;
            if (owner == null)
            {
                return null;
            }

            PersistentPlayerDataService? service = GameHandler.GetService<PersistentPlayerDataService>();
            PersistentPlayerData? playerData = service?.GetPlayerData(owner);
            return playerData?.customizationData;
        }
        catch
        {
            return null;
        }
    }

    private static Texture? SafeTexture(CustomizationOption[]? options, int index)
    {
        if (options == null || index < 0 || index >= options.Length)
        {
            return null;
        }

        CustomizationOption option = options[index];
        if (option == null || option.isBlank || option.texture == null)
        {
            return null;
        }

        return option.texture;
    }

    private static Texture? SafeAccessory(CustomizationOption[]? options, int index)
    {
        if (options == null || index < 0 || index >= options.Length)
        {
            return null;
        }

        CustomizationOption option = options[index];
        if (option == null || option.isBlank || option.isThirdEye || option.texture == null)
        {
            return null;
        }

        return option.texture;
    }
}
