using System;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;

namespace PeakScoreboard;

internal sealed class RespawnService
{
    private readonly ModConfig _config;
    private readonly ManualLogSource _logger;

    public RespawnService(ModConfig config, ManualLogSource logger)
    {
        _config = config;
        _logger = logger;
    }

    public bool CanRespawn(out string reason)
    {
        // Hard rule: only the lobby host may respawn players.
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
        {
            reason = "Only the host can respawn players.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool TryRespawn(Character target, out string message)
    {
        if (target == null)
        {
            message = "No target.";
            return false;
        }

        if (!CanRespawn(out string reason))
        {
            message = reason;
            return false;
        }

        bool dead = target.data != null && (target.data.dead || target.IsGhost);
        if (!dead)
        {
            message = $"{ResolveName(target)} is not dead.";
            return false;
        }

        Vector3 position = ResolveRespawnPosition(target);
        bool applyStatus = _config.ApplyRevivePenalty.Value;

        try
        {
            if (target.IsLocal)
            {
                // Local path: use the same RPC the console Revive() command uses.
                PhotonView view = target.refs != null && target.refs.view != null
                    ? target.refs.view
                    : target.photonView;
                view.RPC("RPCA_ReviveAtPosition", RpcTarget.All, position, applyStatus, -1);
            }
            else if (target.refs != null && target.refs.view != null)
            {
                target.refs.view.RPC("RPCA_ReviveAtPosition", RpcTarget.All, position, applyStatus, -1);
            }
            else if (target.photonView != null)
            {
                target.photonView.RPC("RPCA_ReviveAtPosition", RpcTarget.All, position, applyStatus, -1);
            }
            else
            {
                message = $"No PhotonView for {ResolveName(target)}.";
                return false;
            }

            message = $"Respawning {ResolveName(target)}…";
            _logger.LogInfo($"Requested respawn for '{ResolveName(target)}' at {position} (penalty={applyStatus}).");
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError($"Failed to respawn '{ResolveName(target)}': {exception}");
            message = "Respawn failed. Check the BepInEx log.";
            return false;
        }
    }

    private Vector3 ResolveRespawnPosition(Character target)
    {
        float up = Mathf.Max(0.1f, _config.RespawnVerticalOffset.Value);

        if (_config.RespawnBesideLocal.Value)
        {
            Character? local = Character.localCharacter;
            if (local != null && local != target && local.data != null && !local.data.dead && !local.IsGhost)
            {
                return local.Center + Vector3.up * up;
            }

            Character? alive = FindNearestAlive(exclude: target);
            if (alive != null)
            {
                return alive.Center + Vector3.up * up;
            }
        }

        return target.Center + Vector3.up * up;
    }

    private static Character? FindNearestAlive(Character exclude)
    {
        Character? best = null;
        float bestDistance = float.MaxValue;
        Vector3 origin = exclude.Center;

        foreach (PlayerStatusRow row in PlayerStatusService.Capture())
        {
            if (row.Character == exclude || row.IsDead)
            {
                continue;
            }

            float distance = Vector3.Distance(origin, row.Character.Center);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = row.Character;
            }
        }

        return best;
    }

    private static string ResolveName(Character character) =>
        string.IsNullOrWhiteSpace(character.characterName) ? character.name : character.characterName;
}
