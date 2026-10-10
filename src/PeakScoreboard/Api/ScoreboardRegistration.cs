using System;

namespace PeakScoreboard.Api;

/// <summary>
/// Handle returned from <see cref="ScoreboardApi"/> register methods. Dispose to unregister.
/// </summary>
public sealed class ScoreboardRegistration : IDisposable
{
    private Action? _dispose;

    internal ScoreboardRegistration(Action dispose) =>
        _dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));

    public void Dispose()
    {
        Action? dispose = _dispose;
        _dispose = null;
        dispose?.Invoke();
    }
}
