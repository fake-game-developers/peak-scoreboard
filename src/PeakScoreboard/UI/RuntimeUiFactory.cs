using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeakScoreboard.UI;

internal static class RuntimeUiFactory
{
    internal static readonly Color Backdrop = new(0.035f, 0.043f, 0.047f, 0.55f);
    internal static readonly Color Panel = new(0.10f, 0.115f, 0.12f, 0.96f);
    internal static readonly Color Surface = new(0.16f, 0.175f, 0.18f, 1f);
    internal static readonly Color SurfaceHover = new(0.22f, 0.235f, 0.235f, 1f);
    internal static readonly Color Accent = new(0.30f, 0.78f, 0.58f, 1f);
    internal static readonly Color TextPrimary = new(0.95f, 0.96f, 0.94f, 1f);
    internal static readonly Color TextMuted = new(0.68f, 0.71f, 0.69f, 1f);
    internal static readonly Color Alive = new(0.45f, 0.86f, 0.58f, 1f);
    internal static readonly Color Down = new(0.95f, 0.78f, 0.35f, 1f);
    internal static readonly Color Dead = new(0.94f, 0.45f, 0.42f, 1f);
    internal static readonly Color Stamina = new(0.45f, 0.82f, 0.55f, 1f);
    internal static readonly Color StaminaBonus = new(0.95f, 0.82f, 0.30f, 1f);
    internal static readonly Color StaminaTrack = new(0.08f, 0.09f, 0.095f, 1f);
    internal static readonly Color HostTag = new(0.95f, 0.78f, 0.35f, 1f);

    private static Sprite? _whiteSprite;
    private static Sprite? _circleSprite;

    public static RectTransform CreateRect(string name, Transform parent, params Type[] components)
    {
        Type[] componentTypes = new Type[components.Length + 1];
        componentTypes[0] = typeof(RectTransform);
        Array.Copy(components, 0, componentTypes, 1, components.Length);
        var gameObject = new GameObject(name, componentTypes);
        gameObject.transform.SetParent(parent, false);
        return (RectTransform)gameObject.transform;
    }

    public static void Stretch(RectTransform rect, float left = 0, float right = 0, float bottom = 0, float top = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    public static TMP_FontAsset ResolveFont()
    {
        if (FontFallbackSwapper.instance != null && FontFallbackSwapper.instance.mainBaseFont != null)
        {
            return FontFallbackSwapper.instance.mainBaseFont;
        }

        TMP_FontAsset? font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
            .FirstOrDefault(candidate => candidate.name == "DarumaDropOne-Regular SDF")
            ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();
        if (font == null)
        {
            throw new InvalidOperationException("No TextMeshPro font asset is available.");
        }

        return font;
    }

    public static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    public static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget = true)
    {
        RectTransform rect = CreateRect(name, parent, typeof(Image));
        Image image = rect.GetComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static Button CreateButton(string name, Transform parent, TMP_FontAsset font, string label)
    {
        RectTransform rect = CreateRect(name, parent, typeof(Image), typeof(Button));
        Image image = rect.GetComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.type = Image.Type.Sliced;
        image.color = Surface;

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = image;
        button.colors = CreateButtonColors();

        TextMeshProUGUI text = CreateText("Label", rect, font, 20, TextPrimary, TextAlignmentOptions.Center);
        text.text = label;
        Stretch(text.rectTransform, 4, 4, 2, 2);
        return button;
    }

    public static ColorBlock CreateButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Surface;
        colors.highlightedColor = SurfaceHover;
        colors.pressedColor = Accent;
        colors.selectedColor = SurfaceHover;
        colors.disabledColor = new Color(Surface.r, Surface.g, Surface.b, 0.45f);
        colors.colorMultiplier = 1;
        colors.fadeDuration = 0.08f;
        return colors;
    }

    public static Color StatusColor(PlayerStatusRow row)
    {
        if (row.IsDead)
        {
            return Dead;
        }

        if (row.IsPassedOut)
        {
            return Down;
        }

        return Alive;
    }

    public static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null)
        {
            return _whiteSprite;
        }

        Texture2D texture = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(1, 1, 1, 1));
        return _whiteSprite;
    }

    public static Sprite GetCircleSprite()
    {
        if (_circleSprite != null)
        {
            return _circleSprite;
        }

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
        float radius = (size - 1) * 0.5f;
        float center = radius;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        _circleSprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
        return _circleSprite;
    }

    public static RawImage CreateRawImage(string name, Transform parent)
    {
        RectTransform rect = CreateRect(name, parent, typeof(RawImage));
        RawImage image = rect.GetComponent<RawImage>();
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }
}
