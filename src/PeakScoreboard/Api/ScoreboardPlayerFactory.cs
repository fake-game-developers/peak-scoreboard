namespace PeakScoreboard.Api;

internal static class ScoreboardPlayerFactory
{
    public static ScoreboardPlayer FromRow(PlayerStatusRow row) =>
        new(
            row.Character,
            row.ActorId,
            row.Name,
            row.IsLocal,
            row.IsDead,
            row.IsPassedOut,
            row.StatusLabel,
            row.Stats.HeightMeters,
            row.Stats.DistanceMeters,
            row.Stats.Deaths);
}
