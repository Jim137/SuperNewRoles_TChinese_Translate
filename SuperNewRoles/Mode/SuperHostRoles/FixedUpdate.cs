using System;
using System.Collections.Generic;
using HarmonyLib;
using SuperNewRoles.Helpers;
using SuperNewRoles.Patches;
using SuperNewRoles.Roles;
using SuperNewRoles.Roles.Crewmate;
using SuperNewRoles.Roles.Impostor;
using SuperNewRoles.Roles.Neutral;
using SuperNewRoles.Roles.RoleBases;
using SuperNewRoles.Roles.RoleBases.Interfaces;
using UnityEngine;

namespace SuperNewRoles.Mode.SuperHostRoles;

public static class FixedUpdate
{
    public static void RoleFixedUpdate() { }
    public static void Update()
    {
        ISupportSHR playerSHR = RoleBaseManager.GetLocalRoleBase() as ISupportSHR;
        if (PlayerControl.LocalPlayer.IsRole(RoleId.Sheriff))
        {
            if (RoleClass.Sheriff.KillMaxCount == 0)
            {
                FastDestroyableSingleton<HudManager>.Instance.KillButton.gameObject.SetActive(false);
                CachedPlayer.LocalPlayer.Data.Role.CanUseKillButton = false;
                FastDestroyableSingleton<HudManager>.Instance.KillButton.SetTarget(null);
            }
            else
            {
                FastDestroyableSingleton<HudManager>.Instance.KillButton.gameObject.SetActive(true);
                CachedPlayer.LocalPlayer.Data.Role.CanUseKillButton = true;
                FastDestroyableSingleton<HudManager>.Instance.KillButton.SetTarget(PlayerControlFixedUpdatePatch.SetTarget());
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    FastDestroyableSingleton<HudManager>.Instance.KillButton.DoClick();
                }
            }
        }
        else if
            (!PlayerControl.LocalPlayer.IsImpostor() &&(
            (playerSHR != null && playerSHR.HasKillButtonClient) ||
            PlayerControl.LocalPlayer.IsRole
                (
                    RoleId.JackalSeer,
                    RoleId.MadMaker,
                    RoleId.Egoist,
                    RoleId.Demon,
                    RoleId.Arsonist
                )
            ))
        {
            FastDestroyableSingleton<HudManager>.Instance.KillButton.gameObject.SetActive(true);
            CachedPlayer.LocalPlayer.Data.Role.CanUseKillButton = true;
            FastDestroyableSingleton<HudManager>.Instance.KillButton.SetTarget(PlayerControlFixedUpdatePatch.SetTarget());
            if (Input.GetKeyDown(KeyCode.Q))
            {
                FastDestroyableSingleton<HudManager>.Instance.KillButton.DoClick();
            }
        }
        SetNameUpdate.Postfix(PlayerControl.LocalPlayer);
        if (!AmongUsClient.Instance.AmHost) return;
        ChangeName.FixedUpdate();
        if (PlayerControl.LocalPlayer.IsRole(RoleId.PoliceSurgeon))
        {
            PoliceSurgeon.FixedUpdate();
        }
        foreach (PlayerControl p in BotManager.AllBots)
        {
            p.NetTransform.RpcSnapTo(new Vector2(99999, 99999));
        }
        if (AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Started)
        {
            RoleFixedUpdate();
            BlockTool.FixedUpdate();
        }
    }
}