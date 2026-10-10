using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;

namespace PeakScoreboard.UI;

internal sealed class ScoreboardController : IDisposable
{
    private readonly ModConfig _config;
    private readonly ManualLogSource _logger;
    private readonly RespawnService _respawn;
    private ScoreboardView? _view;
    private bool _visible;
    private float _nextRefreshAt;
    private string _statusMessage = string.Empty;
    private bool _statusIsError;

    public ScoreboardController(ModConfig config, ManualLogSource logger)
    {
        _config = config;
        _logger = logger;
        _respawn = new RespawnService(config, logger);
    }

    public void Attach()
    {
        if (_view != null && _view.Root != null)
        {
            return;
        }

        try
        {
            _view = null;
            ScoreboardAction.EnsureRegistered();
            _view = ScoreboardView.Create();
            _view.OnCloseRequested = () => SetVisible(false);
            _logger.LogInfo("Scoreboard UI ready. Press Scoreboard (default Z) to toggle.");
        }
        catch (Exception exception)
        {
            _logger.LogError($"Failed to create scoreboard UI: {exception}");
        }
    }

    public void Tick()
    {
        if (_view == null || _view.Root == null)
        {
            if (GUIManager.instance != null)
            {
                Attach();
            }

            if (_view == null || _view.Root == null)
            {
                return;
            }
        }

        // MenuWindow may close itself (e.g. pause) — keep our state in sync.
        if (_visible && !_view.Window.IsOpen)
        {
            _visible = false;
            _view.SetChromeVisible(false);
        }

        if (ScoreboardAction.WasPressedThisFrame())
        {
            if (_visible)
            {
                SetVisible(false);
            }
            else if (CanShow())
            {
                SetVisible(true);
            }
        }

        if (!_visible)
        {
            return;
        }

        // Keep PEAK's CursorHandler from winning: we are a MenuWindow, but reinforce each frame.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (Time.unscaledTime >= _nextRefreshAt)
        {
            RefreshRows();
            _nextRefreshAt = Time.unscaledTime + 0.25f;
        }
    }

    public void Dispose()
    {
        if (_visible)
        {
            _view?.Close();
        }

        _view?.Destroy();
        _view = null;
        _visible = false;
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;
        if (_view == null)
        {
            return;
        }

        if (visible)
        {
            _statusMessage = string.Empty;
            _statusIsError = false;
            _view.Open();
            RefreshRows();
            _nextRefreshAt = Time.unscaledTime + 0.25f;
        }
        else
        {
            _view.Close();
        }
    }

    private void RefreshRows()
    {
        if (_view == null)
        {
            return;
        }

        IReadOnlyList<PlayerStatusRow> rows = PlayerStatusService.Capture();
        bool canRespawn = _respawn.CanRespawn(out string reason);
        string subtitle = canRespawn
            ? "Press Scoreboard again or Close · click Respawn on dead scouts"
            : reason;
        _view.SetSubtitle(subtitle);
        _view.BindRows(rows, OnRespawnClicked, canRespawn);

        if (string.IsNullOrEmpty(_statusMessage))
        {
            int alive = 0;
            int dead = 0;
            foreach (PlayerStatusRow row in rows)
            {
                if (row.IsDead)
                {
                    dead++;
                }
                else
                {
                    alive++;
                }
            }

            string hostHint = PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient ? " · you are host" : string.Empty;
            _view.SetStatus($"{alive} alive · {dead} dead{hostHint}", isError: false);
        }
        else
        {
            _view.SetStatus(_statusMessage, _statusIsError);
        }
    }

    private void OnRespawnClicked(PlayerStatusRow row)
    {
        bool ok = _respawn.TryRespawn(row.Character, out string message);
        _statusMessage = message;
        _statusIsError = !ok;
        _view?.SetStatus(_statusMessage, _statusIsError);
        _nextRefreshAt = Time.unscaledTime + 0.1f;
    }

    private bool CanShow()
    {
        if (Time.timeScale <= 0f)
        {
            return false;
        }

        GUIManager? gui = GUIManager.instance;
        if (gui != null && gui.wheelActive)
        {
            return false;
        }

        // Our MenuWindow sets windowBlockingInput while open; ignore that for ourselves.
        if (gui != null && gui.windowBlockingInput && !_visible)
        {
            return false;
        }

        if (GUIManager.InPauseMenu)
        {
            return false;
        }

        return Character.localCharacter != null || PlayerHandler.Exists;
    }
}
