using UnityEngine;
using UnityEngine.UI;

namespace PeakScoreboard.UI;

/// <summary>
/// PEAK unlocks the mouse only while a <see cref="MenuWindow"/> with
/// <see cref="MenuWindow.showCursorWhileOpen"/> is in <see cref="MenuWindow.AllActiveWindows"/>.
/// This window exists so the scoreboard can receive Respawn clicks.
/// </summary>
internal sealed class ScoreboardWindow : MenuWindow
{
    public override bool openOnStart => false;

    public override bool selectOnOpen => false;

    public override Selectable? objectToSelectOnOpen => null;

    public override bool closeOnPause => true;

    public override bool closeOnUICancel => false;

    // Block look/movement so mouse deltas don't spin the camera while clicking Respawn.
    public override bool blocksPlayerInput => true;

    public override bool showCursorWhileOpen => true;

    public override bool autoHideOnClose => true;

    public override GameObject panel => gameObject;

    public bool IsOpen => isOpen;

    public void OpenWindow()
    {
        if (!isOpen)
        {
            Open();
        }
    }

    public void CloseWindow()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            AllActiveWindows.Remove(this);
            if (gameObject.activeSelf)
            {
                Hide();
            }
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
