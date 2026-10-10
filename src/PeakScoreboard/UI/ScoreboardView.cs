using System;
using System.Collections.Generic;
using PeakScoreboard.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeakScoreboard.UI;

internal sealed class ScoreboardView
{
    private readonly CanvasGroup _canvasGroup;
    private readonly TMP_FontAsset _font;
    private readonly TextMeshProUGUI _subtitle;
    private readonly TextMeshProUGUI _status;
    private readonly RectTransform _panel;
    private readonly RectTransform _rowContent;
    private readonly ScrollRect _scroll;
    private readonly ScoreboardHeaderView _header;
    private readonly List<ScoreboardRowView> _rows = new();
    private ScoreboardLayout _layout = ScoreboardLayout.Capture();

    private ScoreboardView(
        GameObject root,
        ScoreboardWindow window,
        CanvasGroup canvasGroup,
        TMP_FontAsset font,
        TextMeshProUGUI subtitle,
        TextMeshProUGUI status,
        RectTransform panel,
        RectTransform rowContent,
        ScrollRect scroll,
        ScoreboardHeaderView header)
    {
        Root = root;
        Window = window;
        _canvasGroup = canvasGroup;
        _font = font;
        _subtitle = subtitle;
        _status = status;
        _panel = panel;
        _rowContent = rowContent;
        _scroll = scroll;
        _header = header;
    }

    public GameObject Root { get; }
    public ScoreboardWindow Window { get; }
    public Action? OnCloseRequested { get; set; }

    public static ScoreboardView Create()
    {
        var root = new GameObject(
            "PeakScoreboardUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(CanvasGroup),
            typeof(GraphicRaycaster));

        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            RuntimeUiFactory.Stretch(rootRect);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 240;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            TMP_FontAsset font = RuntimeUiFactory.ResolveFont();

            Image dim = RuntimeUiFactory.CreateImage("Dim", rootRect, RuntimeUiFactory.Backdrop);
            RuntimeUiFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = false;

            Image panel = RuntimeUiFactory.CreateImage("Panel", rootRect, RuntimeUiFactory.Panel);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1280, 640);

            TextMeshProUGUI title = RuntimeUiFactory.CreateText(
                "Title", panelRect, font, 34, RuntimeUiFactory.TextPrimary, TextAlignmentOptions.MidlineLeft);
            title.text = "Scoreboard";
            title.rectTransform.anchorMin = new Vector2(0, 1);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -18);
            title.rectTransform.sizeDelta = new Vector2(-140, 44);

            Button closeButton = RuntimeUiFactory.CreateButton("Close", panelRect, font, "Close");
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-24, -16);
            closeRect.sizeDelta = new Vector2(96, 36);

            TextMeshProUGUI subtitle = RuntimeUiFactory.CreateText(
                "Subtitle", panelRect, font, 18, RuntimeUiFactory.TextMuted, TextAlignmentOptions.MidlineLeft);
            subtitle.text = "Press Scoreboard again or Close · click Respawn on dead scouts";
            subtitle.rectTransform.anchorMin = new Vector2(0, 1);
            subtitle.rectTransform.anchorMax = new Vector2(1, 1);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1);
            subtitle.rectTransform.anchoredPosition = new Vector2(0, -62);
            subtitle.rectTransform.sizeDelta = new Vector2(-48, 28);

            ScoreboardHeaderView header = ScoreboardHeaderView.Create(panelRect, font);

            TextMeshProUGUI status = RuntimeUiFactory.CreateText(
                "Status", panelRect, font, 17, RuntimeUiFactory.TextMuted, TextAlignmentOptions.MidlineLeft);
            status.text = string.Empty;
            status.rectTransform.anchorMin = new Vector2(0, 0);
            status.rectTransform.anchorMax = new Vector2(1, 0);
            status.rectTransform.pivot = new Vector2(0.5f, 0);
            status.rectTransform.anchoredPosition = new Vector2(0, 16);
            status.rectTransform.sizeDelta = new Vector2(-48, 24);

            RectTransform scrollRoot = RuntimeUiFactory.CreateRect("Rows", panelRect, typeof(ScrollRect));
            scrollRoot.anchorMin = Vector2.zero;
            scrollRoot.anchorMax = Vector2.one;
            scrollRoot.offsetMin = new Vector2(24, 48);
            scrollRoot.offsetMax = new Vector2(-24, -132);

            RectTransform viewport = RuntimeUiFactory.CreateRect("Viewport", scrollRoot, typeof(Image), typeof(RectMask2D));
            RuntimeUiFactory.Stretch(viewport, left: 0, right: 18, bottom: 0, top: 0);
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.GetComponent<Image>().raycastTarget = true;

            RectTransform content = RuntimeUiFactory.CreateRect(
                "Content", viewport, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(0, 0, 0, 0);

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateVerticalScrollbar(scrollRoot);

            ScrollRect scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 4f;

            ScoreboardWindow window = root.AddComponent<ScoreboardWindow>();
            UnityEngine.Object.DontDestroyOnLoad(root);

            var view = new ScoreboardView(
                root,
                window,
                root.GetComponent<CanvasGroup>(),
                font,
                subtitle,
                status,
                panelRect,
                content,
                scroll,
                header);
            closeButton.onClick.AddListener(() => view.OnCloseRequested?.Invoke());
            // Keep the object active so MenuWindow.Start can run StartClosed;
            // visibility is driven by MenuWindow.Open/Close.
            view.ApplyLayout(ScoreboardLayout.Capture());
            view.SetChromeVisible(false);
            return view;
        }
        catch
        {
            root.SetActive(false);
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }

    public void SetChromeVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    public void Open()
    {
        if (Root != null && !Root.activeSelf)
        {
            Root.SetActive(true);
        }

        SetChromeVisible(true);
        Window.OpenWindow();
        _scroll.verticalNormalizedPosition = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        SetChromeVisible(false);
        Window.CloseWindow();
    }

    private static Scrollbar CreateVerticalScrollbar(RectTransform scrollRoot)
    {
        Image track = RuntimeUiFactory.CreateImage(
            "Scrollbar",
            scrollRoot,
            new Color(0.08f, 0.09f, 0.095f, 0.9f));
        RectTransform trackRect = track.rectTransform;
        trackRect.anchorMin = new Vector2(1f, 0f);
        trackRect.anchorMax = new Vector2(1f, 1f);
        trackRect.pivot = new Vector2(1f, 0.5f);
        trackRect.sizeDelta = new Vector2(12f, 0f);
        trackRect.anchoredPosition = Vector2.zero;
        trackRect.offsetMin = new Vector2(-12f, 2f);
        trackRect.offsetMax = new Vector2(0f, -2f);

        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        RectTransform slidingArea = RuntimeUiFactory.CreateRect("Sliding Area", trackRect);
        RuntimeUiFactory.Stretch(slidingArea, 1, 1, 1, 1);

        Image handle = RuntimeUiFactory.CreateImage(
            "Handle",
            slidingArea,
            new Color(0.45f, 0.5f, 0.48f, 1f));
        RuntimeUiFactory.Stretch(handle.rectTransform);

        scrollbar.targetGraphic = handle;
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.value = 1f;
        scrollbar.size = 0.2f;
        return scrollbar;
    }

    public void SetSubtitle(string text) => _subtitle.text = text;

    public void SetStatus(string text, bool isError)
    {
        _status.text = text ?? string.Empty;
        _status.color = isError ? RuntimeUiFactory.Dead : RuntimeUiFactory.TextMuted;
    }

    public void BindRows(IReadOnlyList<PlayerStatusRow> rows, Action<PlayerStatusRow> onRespawn, bool canRespawn)
    {
        EnsureLayoutCurrent();

        while (_rows.Count < rows.Count)
        {
            ScoreboardRowView rowView = ScoreboardRowView.Create(_rowContent, _font);
            rowView.ApplyLayout(_layout);
            _rows.Add(rowView);
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            if (i < rows.Count)
            {
                PlayerStatusRow row = rows[i];
                _rows[i].Bind(_layout, row, () => onRespawn(row), canRespawn && row.CanRespawn);
                _rows[i].SetActive(true);
            }
            else
            {
                _rows[i].SetActive(false);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_rowContent);
        Canvas.ForceUpdateCanvases();
    }

    private void EnsureLayoutCurrent()
    {
        int version = ScoreboardRegistry.Version;
        if (!_layout.NeedsRebuild(version))
        {
            return;
        }

        ApplyLayout(ScoreboardLayout.Capture());
    }

    private void ApplyLayout(ScoreboardLayout layout)
    {
        _layout = layout;
        float width = 1280f + Math.Min(320f, layout.Columns.Count * 72f + layout.RowActions.Count * 40f);
        _panel.sizeDelta = new Vector2(width, 640f);
        _header.ApplyLayout(layout);
        foreach (ScoreboardRowView row in _rows)
        {
            row.ApplyLayout(layout);
        }
    }

    public void Destroy()
    {
        if (Root != null)
        {
            UnityEngine.Object.Destroy(Root);
        }
    }
}

internal sealed class ScoreboardAvatarView
{
    private readonly Image _skin;
    private readonly RawImage _steam;
    private readonly RawImage _eyes;
    private readonly RawImage _mouth;
    private readonly RawImage _accessory;
    private readonly GameObject _faceRoot;

    private ScoreboardAvatarView(
        Image skin,
        RawImage steam,
        GameObject faceRoot,
        RawImage eyes,
        RawImage mouth,
        RawImage accessory)
    {
        _skin = skin;
        _steam = steam;
        _faceRoot = faceRoot;
        _eyes = eyes;
        _mouth = mouth;
        _accessory = accessory;
    }

    public static ScoreboardAvatarView Create(Transform parent)
    {
        RectTransform root = RuntimeUiFactory.CreateRect("Avatar", parent, typeof(RectTransform));
        root.anchorMin = new Vector2(0, 0.5f);
        root.anchorMax = new Vector2(0, 0.5f);
        root.pivot = new Vector2(0, 0.5f);
        root.anchoredPosition = new Vector2(10, 0);
        root.sizeDelta = new Vector2(44, 44);

        Image skin = RuntimeUiFactory.CreateImage("Skin", root, ScoutAvatar.Fallback.SkinColor, raycastTarget: false);
        skin.sprite = RuntimeUiFactory.GetCircleSprite();
        skin.type = Image.Type.Simple;
        RuntimeUiFactory.Stretch(skin.rectTransform);

        // Circular mask for the Steam portrait.
        RectTransform steamMaskRoot = RuntimeUiFactory.CreateRect("SteamMask", root, typeof(Image), typeof(Mask));
        RuntimeUiFactory.Stretch(steamMaskRoot);
        Image steamMask = steamMaskRoot.GetComponent<Image>();
        steamMask.sprite = RuntimeUiFactory.GetCircleSprite();
        steamMask.type = Image.Type.Simple;
        steamMask.color = Color.white;
        steamMask.raycastTarget = false;
        steamMaskRoot.GetComponent<Mask>().showMaskGraphic = false;

        RawImage steam = RuntimeUiFactory.CreateRawImage("Steam", steamMaskRoot);
        RuntimeUiFactory.Stretch(steam.rectTransform);
        steam.enabled = false;

        RectTransform faceRoot = RuntimeUiFactory.CreateRect("Face", root);
        RuntimeUiFactory.Stretch(faceRoot, 4, 4, 4, 4);

        RawImage eyes = RuntimeUiFactory.CreateRawImage("Eyes", faceRoot);
        RuntimeUiFactory.Stretch(eyes.rectTransform);
        eyes.enabled = false;

        RawImage mouth = RuntimeUiFactory.CreateRawImage("Mouth", faceRoot);
        RuntimeUiFactory.Stretch(mouth.rectTransform);
        mouth.enabled = false;

        RawImage accessory = RuntimeUiFactory.CreateRawImage("Accessory", faceRoot);
        RuntimeUiFactory.Stretch(accessory.rectTransform);
        accessory.enabled = false;

        return new ScoreboardAvatarView(skin, steam, faceRoot.gameObject, eyes, mouth, accessory);
    }

    public void Bind(ScoutAvatar avatar)
    {
        _skin.color = avatar.SkinColor;

        if (avatar.HasSteamAvatar)
        {
            _steam.texture = avatar.SteamAvatar;
            _steam.enabled = true;
            _faceRoot.SetActive(false);
            return;
        }

        _steam.enabled = false;
        _steam.texture = null;
        _faceRoot.SetActive(true);

        BindLayer(_eyes, avatar.Eyes);
        BindLayer(_mouth, avatar.Mouth);
        BindLayer(_accessory, avatar.Accessory);
    }

    private static void BindLayer(RawImage image, Texture? texture)
    {
        if (texture == null)
        {
            image.enabled = false;
            image.texture = null;
            return;
        }

        image.texture = texture;
        image.enabled = true;
    }
}

internal sealed class ScoreboardStaminaBar
{
    private const int MaxAfflictionSegments = 8;

    private readonly Image _available;
    private readonly Image _bonus;
    private readonly TextMeshProUGUI _label;
    private readonly List<Image> _segments = new();
    private readonly RectTransform _track;

    private ScoreboardStaminaBar(Image available, Image bonus, TextMeshProUGUI label, RectTransform track)
    {
        _available = available;
        _bonus = bonus;
        _label = label;
        _track = track;
    }

    public static ScoreboardStaminaBar Create(RectTransform parent, TMP_FontAsset font, float minX, float maxX)
    {
        RectTransform root = RuntimeUiFactory.CreateRect("Stamina", parent);
        root.anchorMin = new Vector2(minX, 0);
        root.anchorMax = new Vector2(maxX, 1);
        root.offsetMin = new Vector2(6, 8);
        root.offsetMax = new Vector2(-6, -8);

        Image track = RuntimeUiFactory.CreateImage("Track", root, RuntimeUiFactory.StaminaTrack, raycastTarget: false);
        track.rectTransform.anchorMin = new Vector2(0, 0.42f);
        track.rectTransform.anchorMax = new Vector2(1, 0.82f);
        track.rectTransform.offsetMin = Vector2.zero;
        track.rectTransform.offsetMax = Vector2.zero;

        Image available = RuntimeUiFactory.CreateImage("Available", track.rectTransform, RuntimeUiFactory.Stamina, raycastTarget: false);
        available.rectTransform.anchorMin = Vector2.zero;
        available.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        available.rectTransform.offsetMin = Vector2.zero;
        available.rectTransform.offsetMax = Vector2.zero;

        Image bonus = RuntimeUiFactory.CreateImage("Bonus", root, RuntimeUiFactory.StaminaBonus, raycastTarget: false);
        bonus.rectTransform.anchorMin = new Vector2(0, 0.12f);
        bonus.rectTransform.anchorMax = new Vector2(0.25f, 0.36f);
        bonus.rectTransform.offsetMin = Vector2.zero;
        bonus.rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI label = RuntimeUiFactory.CreateText(
            "Value", root, font, 13, RuntimeUiFactory.TextMuted, TextAlignmentOptions.MidlineLeft);
        label.rectTransform.anchorMin = new Vector2(0, 0);
        label.rectTransform.anchorMax = new Vector2(1, 0.40f);
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;

        return new ScoreboardStaminaBar(available, bonus, label, track.rectTransform);
    }

    public void Bind(PlayerVitals vitals, bool isDead)
    {
        AfflictionIconResolver.EnsureCache();

        if (isDead)
        {
            SetAvailable(0f);
            SetBonus(0f);
            SetSegmentCount(0);
            _label.text = "—";
            return;
        }

        // PEAK full bar = 1.0. Green = absolute free stamina; colored segments = each status.
        float cursor = 0f;
        float available = Mathf.Clamp01(vitals.AvailableStamina);
        SetAvailableRange(cursor, cursor + available);
        cursor += available;

        int segmentCount = Mathf.Min(vitals.Effects.Count, MaxAfflictionSegments);
        SetSegmentCount(segmentCount);
        for (int i = 0; i < segmentCount; i++)
        {
            PlayerEffect effect = vitals.Effects[i];
            float width = Mathf.Clamp01(effect.Amount);
            Image segment = _segments[i];
            segment.color = AfflictionIconResolver.GetColor(effect.Type);
            SetAnchors(segment.rectTransform, cursor, cursor + width);
            segment.gameObject.SetActive(width > 0.001f);
            cursor += width;
        }

        float bonus = vitals.ExtraStaminaFraction;
        SetBonus(bonus);

        // Mirror HUD numbers: free stamina points, then each status points.
        var parts = new List<string>(4) { vitals.AvailablePoints.ToString() };
        foreach (PlayerEffect effect in vitals.Effects)
        {
            parts.Add(effect.Points.ToString());
            if (parts.Count >= 4)
            {
                break;
            }
        }

        if (bonus > 0.01f)
        {
            _label.text = string.Join(" · ", parts) + $" +{Mathf.RoundToInt(bonus * 100f)}";
        }
        else
        {
            _label.text = string.Join(" · ", parts);
        }
    }

    private void SetAvailable(float value) => SetAvailableRange(0f, value);

    private void SetAvailableRange(float min, float max)
    {
        SetAnchors(_available.rectTransform, min, max);
        _available.gameObject.SetActive(max - min > 0.001f);
    }

    private void SetBonus(float fraction)
    {
        _bonus.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 0.36f);
        _bonus.gameObject.SetActive(fraction > 0.01f);
    }

    private void SetSegmentCount(int count)
    {
        while (_segments.Count < count)
        {
            Image segment = RuntimeUiFactory.CreateImage(
                "Affliction",
                _track,
                Color.white,
                raycastTarget: false);
            segment.rectTransform.offsetMin = Vector2.zero;
            segment.rectTransform.offsetMax = Vector2.zero;
            _segments.Add(segment);
        }

        for (int i = 0; i < _segments.Count; i++)
        {
            _segments[i].gameObject.SetActive(i < count);
        }
    }

    private static void SetAnchors(RectTransform rect, float minX, float maxX)
    {
        rect.anchorMin = new Vector2(Mathf.Clamp01(minX), 0f);
        rect.anchorMax = new Vector2(Mathf.Clamp01(maxX), 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

internal sealed class ScoreboardEffectsIcons
{
    private const int MaxVisible = 4;
    private const float ChipWidth = 34f;
    private const float IconSize = 22f;

    private readonly RectTransform _root;
    private readonly TextMeshProUGUI _empty;
    private readonly TextMeshProUGUI _overflow;
    private readonly TMP_FontAsset _font;
    private readonly List<EffectChip> _chips = new();

    private sealed class EffectChip
    {
        public GameObject Root = null!;
        public Image Icon = null!;
        public TextMeshProUGUI Label = null!;
    }

    private ScoreboardEffectsIcons(
        RectTransform root,
        TextMeshProUGUI empty,
        TextMeshProUGUI overflow,
        TMP_FontAsset font)
    {
        _root = root;
        _empty = empty;
        _overflow = overflow;
        _font = font;
    }

    public static ScoreboardEffectsIcons Create(RectTransform parent, TMP_FontAsset font, float minX, float maxX)
    {
        RectTransform root = RuntimeUiFactory.CreateRect(
            "Effects", parent, typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        root.anchorMin = new Vector2(minX, 0);
        root.anchorMax = new Vector2(maxX, 1);
        root.offsetMin = new Vector2(2, 6);
        root.offsetMax = new Vector2(-2, -6);

        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 4;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 0, 0, 0);

        root.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI empty = RuntimeUiFactory.CreateText(
            "Empty", root, font, 16, RuntimeUiFactory.TextMuted, TextAlignmentOptions.MidlineLeft);
        empty.text = "—";
        empty.rectTransform.sizeDelta = new Vector2(24, IconSize);

        TextMeshProUGUI overflow = RuntimeUiFactory.CreateText(
            "Overflow", root, font, 14, RuntimeUiFactory.TextMuted, TextAlignmentOptions.MidlineLeft);
        overflow.text = string.Empty;
        overflow.gameObject.SetActive(false);
        overflow.rectTransform.sizeDelta = new Vector2(28, IconSize);

        return new ScoreboardEffectsIcons(root, empty, overflow, font);
    }

    public void Bind(IReadOnlyList<PlayerEffect> effects, bool isDead)
    {
        AfflictionIconResolver.EnsureCache();

        if (isDead || effects.Count == 0)
        {
            SetChipCount(0);
            _empty.gameObject.SetActive(true);
            _empty.text = "—";
            _overflow.gameObject.SetActive(false);
            return;
        }

        _empty.gameObject.SetActive(false);

        int visible = Mathf.Min(effects.Count, MaxVisible);
        SetChipCount(visible);

        for (int i = 0; i < visible; i++)
        {
            PlayerEffect effect = effects[i];
            EffectChip chip = _chips[i];
            chip.Root.SetActive(true);
            chip.Root.name = AfflictionIconResolver.DisplayName(effect.Type);

            if (AfflictionIconResolver.TryGet(effect.Type, out Sprite? sprite, out _) && sprite != null)
            {
                // Status_* sprites are already colored — don't multiply with bar tint.
                chip.Icon.sprite = sprite;
                chip.Icon.color = Color.white;
                chip.Icon.preserveAspect = true;
            }
            else
            {
                chip.Icon.sprite = RuntimeUiFactory.GetCircleSprite();
                chip.Icon.color = AfflictionIconResolver.GetColor(effect.Type);
                chip.Icon.preserveAspect = true;
            }

            chip.Label.text = effect.Points.ToString();
            chip.Label.color = AfflictionIconResolver.GetColor(effect.Type);
        }

        int extra = effects.Count - visible;
        if (extra > 0)
        {
            _overflow.text = $"+{extra}";
            _overflow.gameObject.SetActive(true);
        }
        else
        {
            _overflow.gameObject.SetActive(false);
        }
    }

    private void SetChipCount(int count)
    {
        while (_chips.Count < count)
        {
            _chips.Add(CreateChip());
        }

        for (int i = 0; i < _chips.Count; i++)
        {
            _chips[i].Root.SetActive(i < count);
        }
    }

    private EffectChip CreateChip()
    {
        RectTransform chipRoot = RuntimeUiFactory.CreateRect("EffectChip", _root, typeof(LayoutElement));
        chipRoot.sizeDelta = new Vector2(ChipWidth, 44f);
        LayoutElement layout = chipRoot.GetComponent<LayoutElement>();
        layout.preferredWidth = ChipWidth;
        layout.preferredHeight = 44f;
        layout.minWidth = ChipWidth;
        layout.minHeight = 44f;
        chipRoot.SetSiblingIndex(_overflow.transform.GetSiblingIndex());

        Image icon = RuntimeUiFactory.CreateImage("Icon", chipRoot, Color.white, raycastTarget: false);
        icon.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        icon.rectTransform.pivot = new Vector2(0.5f, 1f);
        icon.rectTransform.anchoredPosition = new Vector2(0f, -2f);
        icon.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);

        TextMeshProUGUI label = RuntimeUiFactory.CreateText(
            "Points", chipRoot, _font, 14, RuntimeUiFactory.TextPrimary, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = new Vector2(0f, 0f);
        label.rectTransform.anchorMax = new Vector2(1f, 0f);
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        label.rectTransform.sizeDelta = new Vector2(0f, 16f);

        return new EffectChip
        {
            Root = chipRoot.gameObject,
            Icon = icon,
            Label = label,
        };
    }
}

internal sealed class ScoreboardHeaderView
{
    private readonly RectTransform _root;
    private readonly TextMeshProUGUI _scout;
    private readonly TextMeshProUGUI _state;
    private readonly TextMeshProUGUI _stamina;
    private readonly TextMeshProUGUI _effects;
    private readonly TextMeshProUGUI _height;
    private readonly TextMeshProUGUI _travel;
    private readonly TextMeshProUGUI _deaths;
    private readonly ScoreboardExtensionCells _extensions;

    private ScoreboardHeaderView(
        RectTransform root,
        TextMeshProUGUI scout,
        TextMeshProUGUI state,
        TextMeshProUGUI stamina,
        TextMeshProUGUI effects,
        TextMeshProUGUI height,
        TextMeshProUGUI travel,
        TextMeshProUGUI deaths,
        ScoreboardExtensionCells extensions)
    {
        _root = root;
        _scout = scout;
        _state = state;
        _stamina = stamina;
        _effects = effects;
        _height = height;
        _travel = travel;
        _deaths = deaths;
        _extensions = extensions;
    }

    public static ScoreboardHeaderView Create(RectTransform panel, TMP_FontAsset font)
    {
        RectTransform header = RuntimeUiFactory.CreateRect("Header", panel, typeof(Image));
        Image background = header.GetComponent<Image>();
        background.sprite = RuntimeUiFactory.GetWhiteSprite();
        background.type = Image.Type.Sliced;
        background.color = new Color(0.13f, 0.145f, 0.15f, 1f);
        background.raycastTarget = false;
        header.anchorMin = new Vector2(0, 1);
        header.anchorMax = new Vector2(1, 1);
        header.pivot = new Vector2(0.5f, 1);
        header.anchoredPosition = new Vector2(0, -96);
        header.sizeDelta = new Vector2(-48, 28);

        TextMeshProUGUI scout = PlaceLabel(header, font, "Scout", 0.00f, 0.22f, TextAlignmentOptions.MidlineLeft, muted: true, leftPad: 58);
        TextMeshProUGUI state = PlaceLabel(header, font, "State", 0.22f, 0.30f, TextAlignmentOptions.MidlineLeft, muted: true);
        TextMeshProUGUI stamina = PlaceLabel(header, font, "Stamina", 0.30f, 0.48f, TextAlignmentOptions.MidlineLeft, muted: true);
        TextMeshProUGUI effects = PlaceLabel(header, font, "Effects", 0.48f, 0.66f, TextAlignmentOptions.MidlineLeft, muted: true);
        TextMeshProUGUI height = PlaceLabel(header, font, "Height", 0.66f, 0.75f, TextAlignmentOptions.MidlineRight, muted: true);
        TextMeshProUGUI travel = PlaceLabel(header, font, "Travel", 0.75f, 0.84f, TextAlignmentOptions.MidlineRight, muted: true);
        TextMeshProUGUI deaths = PlaceLabel(header, font, "Deaths", 0.84f, 0.90f, TextAlignmentOptions.Center, muted: true);

        return new ScoreboardHeaderView(
            header,
            scout,
            state,
            stamina,
            effects,
            height,
            travel,
            deaths,
            new ScoreboardExtensionCells(header, font, header: true));
    }

    public void ApplyLayout(ScoreboardLayout layout)
    {
        ApplyLabelSlot(_scout, layout.Scout, leftPad: 58f);
        ApplyLabelSlot(_state, layout.State);
        ApplyLabelSlot(_stamina, layout.Stamina);
        ApplyLabelSlot(_effects, layout.Effects);
        ApplyLabelSlot(_height, layout.Height);
        ApplyLabelSlot(_travel, layout.Travel);
        ApplyLabelSlot(_deaths, layout.Deaths);
        _extensions.BindHeader(layout);
    }

    private static void ApplyLabelSlot(TextMeshProUGUI label, ScoreboardLayout.Slot slot, float leftPad = 8f)
    {
        label.rectTransform.anchorMin = new Vector2(slot.Min, 0f);
        label.rectTransform.anchorMax = new Vector2(slot.Max, 1f);
        label.rectTransform.offsetMin = new Vector2(leftPad, 4f);
        label.rectTransform.offsetMax = new Vector2(-8f, -4f);
    }

    private static TextMeshProUGUI PlaceLabel(
        RectTransform parent,
        TMP_FontAsset font,
        string text,
        float anchorMinX,
        float anchorMaxX,
        TextAlignmentOptions alignment,
        bool muted = false,
        float leftPad = 8f)
    {
        TextMeshProUGUI label = RuntimeUiFactory.CreateText(
            "Cell",
            parent,
            font,
            muted ? 16f : 20f,
            muted ? RuntimeUiFactory.TextMuted : RuntimeUiFactory.TextPrimary,
            alignment);
        label.text = text;
        label.rectTransform.anchorMin = new Vector2(anchorMinX, 0);
        label.rectTransform.anchorMax = new Vector2(anchorMaxX, 1);
        label.rectTransform.offsetMin = new Vector2(leftPad, 4);
        label.rectTransform.offsetMax = new Vector2(-8, -4);
        return label;
    }
}

internal sealed class ScoreboardRowView
{
    private readonly GameObject _root;
    private readonly ScoreboardAvatarView _avatar;
    private readonly TextMeshProUGUI _name;
    private readonly TextMeshProUGUI _status;
    private readonly ScoreboardStaminaBar _stamina;
    private readonly RectTransform _staminaRoot;
    private readonly ScoreboardEffectsIcons _effects;
    private readonly RectTransform _effectsRoot;
    private readonly TextMeshProUGUI _height;
    private readonly TextMeshProUGUI _distance;
    private readonly TextMeshProUGUI _deaths;
    private readonly Button _respawn;
    private readonly Image _background;
    private readonly ScoreboardExtensionCells _extensions;

    private ScoreboardRowView(
        GameObject root,
        Image background,
        ScoreboardAvatarView avatar,
        TextMeshProUGUI name,
        TextMeshProUGUI status,
        ScoreboardStaminaBar stamina,
        RectTransform staminaRoot,
        ScoreboardEffectsIcons effects,
        RectTransform effectsRoot,
        TextMeshProUGUI height,
        TextMeshProUGUI distance,
        TextMeshProUGUI deaths,
        Button respawn,
        ScoreboardExtensionCells extensions)
    {
        _root = root;
        _background = background;
        _avatar = avatar;
        _name = name;
        _status = status;
        _stamina = stamina;
        _staminaRoot = staminaRoot;
        _effects = effects;
        _effectsRoot = effectsRoot;
        _height = height;
        _distance = distance;
        _deaths = deaths;
        _respawn = respawn;
        _extensions = extensions;
    }

    public static ScoreboardRowView Create(Transform parent, TMP_FontAsset font)
    {
        RectTransform root = RuntimeUiFactory.CreateRect("Row", parent, typeof(Image), typeof(LayoutElement));
        Image background = root.GetComponent<Image>();
        background.sprite = RuntimeUiFactory.GetWhiteSprite();
        background.type = Image.Type.Sliced;
        background.color = RuntimeUiFactory.Surface;
        root.GetComponent<LayoutElement>().preferredHeight = 62;
        root.GetComponent<LayoutElement>().minHeight = 62;

        ScoreboardAvatarView avatar = ScoreboardAvatarView.Create(root);
        TextMeshProUGUI name = PlaceLabel(root, font, "", 0.00f, 0.22f, TextAlignmentOptions.MidlineLeft, leftPad: 58);
        TextMeshProUGUI status = PlaceLabel(root, font, "", 0.22f, 0.30f, TextAlignmentOptions.MidlineLeft);
        ScoreboardStaminaBar stamina = ScoreboardStaminaBar.Create(root, font, 0.30f, 0.48f);
        RectTransform staminaRoot = root.Find("Stamina") as RectTransform
            ?? throw new InvalidOperationException("Stamina root missing.");
        ScoreboardEffectsIcons effects = ScoreboardEffectsIcons.Create(root, font, 0.48f, 0.66f);
        RectTransform effectsRoot = root.Find("Effects") as RectTransform
            ?? throw new InvalidOperationException("Effects root missing.");
        TextMeshProUGUI height = PlaceLabel(root, font, "", 0.66f, 0.75f, TextAlignmentOptions.MidlineRight);
        TextMeshProUGUI distance = PlaceLabel(root, font, "", 0.75f, 0.84f, TextAlignmentOptions.MidlineRight);
        TextMeshProUGUI deaths = PlaceLabel(root, font, "", 0.84f, 0.90f, TextAlignmentOptions.Center);

        Button respawn = RuntimeUiFactory.CreateButton("Respawn", root, font, "Respawn");
        RectTransform respawnRect = respawn.GetComponent<RectTransform>();
        respawnRect.anchorMin = new Vector2(0.88f, 0.5f);
        respawnRect.anchorMax = new Vector2(0.98f, 0.5f);
        respawnRect.pivot = new Vector2(0.5f, 0.5f);
        respawnRect.anchoredPosition = Vector2.zero;
        respawnRect.sizeDelta = new Vector2(0, 36);
        respawnRect.offsetMin = new Vector2(respawnRect.offsetMin.x, -18);
        respawnRect.offsetMax = new Vector2(respawnRect.offsetMax.x, 18);

        return new ScoreboardRowView(
            root.gameObject,
            background,
            avatar,
            name,
            status,
            stamina,
            staminaRoot,
            effects,
            effectsRoot,
            height,
            distance,
            deaths,
            respawn,
            new ScoreboardExtensionCells(root, font, header: false));
    }

    public void ApplyLayout(ScoreboardLayout layout)
    {
        ApplyLabelSlot(_name, layout.Scout, leftPad: 58f);
        ApplyLabelSlot(_status, layout.State);
        ApplyAnchoredSlot(_staminaRoot, layout.Stamina, padX: 6f, padY: 8f);
        ApplyAnchoredSlot(_effectsRoot, layout.Effects, padX: 2f, padY: 6f);
        ApplyLabelSlot(_height, layout.Height);
        ApplyLabelSlot(_distance, layout.Travel);
        ApplyLabelSlot(_deaths, layout.Deaths);
        ScoreboardExtensionCells.PlaceRespawnButton(_respawn.GetComponent<RectTransform>(), layout);
        _extensions.SyncStructure(layout);
    }

    public void Bind(ScoreboardLayout layout, PlayerStatusRow row, Action onRespawn, bool showRespawn)
    {
        ApplyLayout(layout);

        _avatar.Bind(PlayerAvatarResolver.Resolve(row.Character));
        _name.text = FormatName(row);
        _name.color = row.Vitals.IsHost ? RuntimeUiFactory.HostTag : RuntimeUiFactory.TextPrimary;
        _status.text = row.StatusLabel;
        _status.color = RuntimeUiFactory.StatusColor(row);
        _stamina.Bind(row.Vitals, row.IsDead);
        _effects.Bind(row.Vitals.Effects, row.IsDead);
        _height.text = FormatMeters(row.Stats.HeightMeters);
        _distance.text = FormatMeters(row.Stats.DistanceMeters);
        _deaths.text = row.Stats.Deaths.ToString();
        _background.color = RuntimeUiFactory.Surface;

        ScoreboardPlayer player = ScoreboardPlayerFactory.FromRow(row);
        _extensions.BindRow(layout, player);

        _respawn.onClick.RemoveAllListeners();
        _respawn.gameObject.SetActive(showRespawn);
        if (showRespawn)
        {
            _respawn.onClick.AddListener(() => onRespawn());
        }
    }

    public void SetActive(bool active) => _root.SetActive(active);

    private static string FormatName(PlayerStatusRow row)
    {
        string name = row.Name;
        if (row.IsLocal)
        {
            name += " (you)";
        }

        if (row.Vitals.IsHost)
        {
            name += " [HOST]";
        }

        return name;
    }

    private static TextMeshProUGUI PlaceLabel(
        RectTransform parent,
        TMP_FontAsset font,
        string text,
        float anchorMinX,
        float anchorMaxX,
        TextAlignmentOptions alignment,
        bool muted = false,
        float leftPad = 8f,
        float fontSize = 0f)
    {
        float size = fontSize > 0f ? fontSize : muted ? 16f : 20f;
        TextMeshProUGUI label = RuntimeUiFactory.CreateText(
            "Cell",
            parent,
            font,
            size,
            muted ? RuntimeUiFactory.TextMuted : RuntimeUiFactory.TextPrimary,
            alignment);
        label.text = text;
        label.rectTransform.anchorMin = new Vector2(anchorMinX, 0);
        label.rectTransform.anchorMax = new Vector2(anchorMaxX, 1);
        label.rectTransform.offsetMin = new Vector2(leftPad, 4);
        label.rectTransform.offsetMax = new Vector2(-8, -4);
        return label;
    }

    private static void ApplyLabelSlot(TextMeshProUGUI label, ScoreboardLayout.Slot slot, float leftPad = 8f)
    {
        label.rectTransform.anchorMin = new Vector2(slot.Min, 0f);
        label.rectTransform.anchorMax = new Vector2(slot.Max, 1f);
        label.rectTransform.offsetMin = new Vector2(leftPad, 4f);
        label.rectTransform.offsetMax = new Vector2(-8f, -4f);
    }

    private static void ApplyAnchoredSlot(RectTransform rect, ScoreboardLayout.Slot slot, float padX, float padY)
    {
        rect.anchorMin = new Vector2(slot.Min, 0f);
        rect.anchorMax = new Vector2(slot.Max, 1f);
        rect.offsetMin = new Vector2(padX, padY);
        rect.offsetMax = new Vector2(-padX, -padY);
    }

    private static string FormatMeters(float meters)
    {
        if (meters >= 1000f)
        {
            return $"{meters / 1000f:0.0} km";
        }

        return $"{Mathf.RoundToInt(meters)} m";
    }
}
