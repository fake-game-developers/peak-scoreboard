using System;
using TMPro;
using UnityEngine;
using Zorro.ControllerSupport;
using Zorro.Core;

namespace PeakScoreboard.UI;

/// <summary>
/// Replaces the vanilla <see cref="InputIcon"/> glyph on our cloned CONTROLS row.
/// InputIcon is driven by a fixed <see cref="InputSpriteData.InputAction"/> enum (no Scoreboard
/// entry), so clones kept showing the template action (e.g. MoveForward = W).
/// </summary>
internal sealed class ScoreboardBindingGlyph : MonoBehaviour
{
    private TMP_Text? _text;
    private bool _subscribed;

    public void Bind(TMP_Text text)
    {
        _text = text;
        Refresh(InputHandler.GetCurrentUsedInputScheme());
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh(InputHandler.GetCurrentUsedInputScheme());
    }

    private void OnDisable() => Unsubscribe();

    private void OnDestroy() => Unsubscribe();

    private void Subscribe()
    {
        if (_subscribed)
        {
            return;
        }

        InputHandler? handler = RetrievableResourceSingleton<InputHandler>.Instance;
        if (handler == null)
        {
            return;
        }

        handler.InputSchemeChanged += OnDeviceChange;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
        {
            return;
        }

        InputHandler? handler = RetrievableResourceSingleton<InputHandler>.Instance;
        if (handler != null)
        {
            handler.InputSchemeChanged -= OnDeviceChange;
        }

        _subscribed = false;
    }

    private void OnDeviceChange(InputScheme scheme) => Refresh(scheme);

    public void Refresh(InputScheme scheme)
    {
        if (_text == null)
        {
            return;
        }

        try
        {
            string path = InputSpriteData.GetBindingPath(ScoreboardAction.ActionName, scheme, out _);
            InputSpriteData? sprites = SingletonAsset<InputSpriteData>.Instance;
            if (sprites == null)
            {
                _text.text = FormatFallback(path);
                return;
            }

            string tag = scheme == InputScheme.Gamepad
                ? sprites.GetSpriteTagFromInputPathGamepad(path)
                : sprites.GetSpriteTagFromInputPathKeyboard(path);

            _text.text = string.IsNullOrEmpty(tag) ? FormatFallback(path) : tag;
        }
        catch (Exception)
        {
            _text.text = FormatFallback(ScoreboardAction.GetEffectiveKeyboardPath());
        }
    }

    private static string FormatFallback(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "?";
        }

        int slash = path.LastIndexOf('/');
        string end = slash >= 0 && slash < path.Length - 1 ? path[(slash + 1)..] : path;
        return end.Length == 1 ? end.ToUpperInvariant() : end;
    }
}
