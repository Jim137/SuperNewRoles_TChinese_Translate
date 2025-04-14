using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using SuperNewRoles.Modules;
using UnityEngine.Networking.Types;
using UnityEngine;
using SuperNewRoles.Roles.Ability;
using SuperNewRoles.CustomOptions;

namespace SuperNewRoles.Roles;

internal abstract class GhostRoleBase<T> : BaseSingleton<T>, IGhostRoleBase where T : GhostRoleBase<T>, new()
{
    protected int _optionidbase = -1;

    public abstract GhostRoleId Role { get; }
    public abstract Color32 RoleColor { get; }
    public abstract List<Func<AbilityBase>> Abilities { get; }
    public abstract QuoteMod QuoteMod { get; }
    public abstract AssignedTeamType AssignedTeam { get; }
    public abstract WinnerTeamType WinnerTeam { get; }
    public abstract TeamTag TeamTag { get; }
    public abstract RoleTag[] RoleTags { get; }
    public abstract short IntroNum { get; }
    public virtual RoleId[] RelatedRoleIds { get; } = [];
    public virtual bool HiddenOption => false;

    // public abstract void CreateCustomOption();
}

public interface IGhostRoleBase
{
    public GhostRoleId Role { get; }
    public Color32 RoleColor { get; }
    /// <summary>
    /// 追加したいAbilityを[ typeof(HogeAbility), typeof(FugaAbility) ]の形でListとして用意する
    /// 役職選出時(OnSetRole)に自動でnewされます
    /// </summary>
    public List<Func<AbilityBase>> Abilities { get; }
    public QuoteMod QuoteMod { get; }
    public AssignedTeamType AssignedTeam { get; }
    public WinnerTeamType WinnerTeam { get; }
    public TeamTag TeamTag { get; }
    public RoleTag[] RoleTags { get; }
    public short IntroNum { get; }
    public bool IsVanillaRole => QuoteMod == QuoteMod.Vanilla;
    public RoleId[] RelatedRoleIds { get; }
    public bool HiddenOption { get; }

    /// <summary>
    /// AbilityはAbilitiesから自動でセットされるが、追加で他の処理を行いたい場合はOverrideすること
    /// </summary>
    public virtual void OnSetRole(PlayerControl player)
    {
        ExPlayerControl exPlayer = player;
        AbilityParentGhostRole parent = new(exPlayer, this);
        foreach (var abilityFactory in Abilities)
        {
            exPlayer.AddAbility(abilityFactory(), parent);
        }
    }
}