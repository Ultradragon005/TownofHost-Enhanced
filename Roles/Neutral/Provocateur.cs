﻿using TOHE.Roles.Double;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Provocateur : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 15100;
    private static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => Playerids.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem ProvKillCD;

    public static readonly Dictionary<byte, byte> Provoked = [];

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Provocateur);
        ProvKillCD = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 100f, 2.5f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Provocateur])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        Playerids.Clear();
        Provoked.Clear();
    }
    public override void Add(byte playerId)
    {
        Playerids.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ProvKillCD.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceMini), GetString("CantBoom")));
            return false;
        }
        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.PissedOff;
        killer.RpcMurderPlayer(target);
        killer.RpcMurderPlayer(killer);
        killer.SetRealKiller(target);
        Provoked.TryAdd(killer.PlayerId, target.PlayerId);
        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("ProvocateurButtonText"));
    }
}
