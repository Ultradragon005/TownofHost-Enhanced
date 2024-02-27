using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class BountyHunter : RoleBase
{
    private const int Id = 800;
    private static List<byte> playerIdList = [];
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;

    private static OptionItem OptionTargetChangeTime;
    private static OptionItem OptionSuccessKillCooldown;
    private static OptionItem OptionFailureKillCooldown;
    private static OptionItem OptionShowTargetArrow;

    private static float TargetChangeTime;
    private static float SuccessKillCooldown;
    private static float FailureKillCooldown;
    private static bool ShowTargetArrow;

    private static Dictionary<byte, byte> Targets = [];
    public static Dictionary<byte, float> ChangeTimer = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.BountyHunter);
        OptionTargetChangeTime = FloatOptionItem.Create(Id + 10, "BountyTargetChangeTime", new(10f, 180f, 2.5f), 60f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.BountyHunter])
            .SetValueFormat(OptionFormat.Seconds);
        OptionSuccessKillCooldown = FloatOptionItem.Create(Id + 11, "BountySuccessKillCooldown", new(0f, 180f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.BountyHunter])
            .SetValueFormat(OptionFormat.Seconds);
        OptionFailureKillCooldown = FloatOptionItem.Create(Id + 12, "BountyFailureKillCooldown", new(0f, 180f, 2.5f), 50f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.BountyHunter])
            .SetValueFormat(OptionFormat.Seconds);
        OptionShowTargetArrow = BooleanOptionItem.Create(Id + 13, "BountyShowTargetArrow", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.BountyHunter]);
    }
    public override void Init()
    {
        playerIdList = [];
        On = false;

        Targets = [];
        ChangeTimer = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        On = true;

        TargetChangeTime = OptionTargetChangeTime.GetFloat();
        SuccessKillCooldown = OptionSuccessKillCooldown.GetFloat();
        FailureKillCooldown = OptionFailureKillCooldown.GetFloat();
        ShowTargetArrow = OptionShowTargetArrow.GetBool();

        if (AmongUsClient.Instance.AmHost)
        {
            ResetTarget(Utils.GetPlayerById(playerId));
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateLowLoadOthers);
        }
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    private static void SendRPC(byte bountyId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetBountyTarget, SendOption.Reliable, -1);
        writer.Write(bountyId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte bountyId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        Targets[bountyId] = targetId;
        if (ShowTargetArrow) TargetArrow.Add(bountyId, targetId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = TargetChangeTime;
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (GetTarget(killer) == target.PlayerId)
        {
            Logger.Info($"{killer?.Data?.PlayerName}: kill target", "BountyHunter");
            Main.AllPlayerKillCooldown[killer.PlayerId] = SuccessKillCooldown;
            killer.SyncSettings();
            ResetTarget(killer);
        }
        else
        {
            Logger.Info($"{killer?.Data?.PlayerName}: non-target kills", "BountyHunter");
            Main.AllPlayerKillCooldown[killer.PlayerId] = FailureKillCooldown;
            killer.SyncSettings();
        }

        return true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target) => ChangeTimer.Clear();
    public static void OnFixedUpdateLowLoadOthers(PlayerControl player)
    {
        if (!ChangeTimer.ContainsKey(player.PlayerId)) return;

        if (!player.IsAlive())
            ChangeTimer.Remove(player.PlayerId);
        else
        {
            var targetId = GetTarget(player);
            if (ChangeTimer[player.PlayerId] >= TargetChangeTime)
            {
                ResetTarget(player);
                Utils.NotifyRoles(SpecifySeer: player, ForceLoop: true);
            }
            if (ChangeTimer[player.PlayerId] >= 0)
                ChangeTimer[player.PlayerId] += Time.fixedDeltaTime;

            if (Main.PlayerStates[targetId].IsDead)
            {
                ResetTarget(player);
                Logger.Info($"player {player.GetNameWithRole()}: target was invalid, so target was updated", "BountyHunter");
                Utils.NotifyRoles(SpecifySeer: player, ForceLoop: true);
            }
        }
    }
    public static byte GetTarget(PlayerControl player)
    {
        if (player == null) return 0xff;
        Targets ??= [];

        if (!Targets.TryGetValue(player.PlayerId, out var targetId))
            targetId = ResetTarget(player);
        return targetId;
    }
    public static PlayerControl GetTargetPC(PlayerControl player)
    {
        var targetId = GetTarget(player);
        return targetId == 0xff ? null : Utils.GetPlayerById(targetId);
    }
    private static bool PotentialTarget(PlayerControl player, PlayerControl target)
    {
        if (target == null || player == null) return false;

        if (player.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) return false;

        if (target.Is(CustomRoles.Romantic)
            && ((Romantic.BetPlayer.TryGetValue(target.PlayerId, out byte romanticPartner) && romanticPartner == player.PlayerId))) return false;

        if (target.Is(CustomRoles.Lawyer)
            && ((Lawyer.Target.TryGetValue(target.PlayerId, out byte lawyerTarget) && lawyerTarget == player.PlayerId) && Lawyer.TargetKnowsLawyer.GetBool())) return false;

        if (player.Is(CustomRoles.Charmed)
            && (target.Is(CustomRoles.Succubus) || (target.Is(CustomRoles.Charmed) && Succubus.TargetKnowOtherTarget.GetBool()))) return false;

        if (player.Is(CustomRoles.Infected)
            && (target.Is(CustomRoles.Infectious) || (target.Is(CustomRoles.Infected) && Infectious.TargetKnowOtherTarget.GetBool()))) return false;

        if (player.Is(CustomRoles.Recruit)
            && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick))) return false;

        if (player.Is(CustomRoles.Contagious)
            && target.Is(CustomRoles.Virus) || (target.Is(CustomRoles.Contagious) && Virus.TargetKnowOtherTarget.GetBool())) return false;

        if (player.Is(CustomRoles.Admired)
            && target.Is(CustomRoles.Admirer) || target.Is(CustomRoles.Admired)) return false;

        if (player.Is(CustomRoles.Soulless)
            && target.Is(CustomRoles.CursedSoul) || target.Is(CustomRoles.Soulless)) return false;

        if (target.GetCustomRole().IsImpostor()
            || ((target.GetCustomRole().IsMadmate() || target.Is(CustomRoles.Madmate)) && Madmate.ImpKnowWhosMadmate.GetBool())) return false;

        return true;

    }
    private static byte ResetTarget(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return 0xff;

        var playerId = player.PlayerId;

        ChangeTimer[playerId] = 0f;

        Logger.Info($"{player.GetNameWithRole()}: reset target", "BountyHunter");
        player.RpcResetAbilityCooldown();

        var cTargets = new List<PlayerControl>(Main.AllAlivePlayerControls.Where(pc => PotentialTarget(player, pc)));

        if (cTargets.Count >= 2 && Targets.TryGetValue(player.PlayerId, out var nowTarget))
            cTargets.RemoveAll(x => x.PlayerId == nowTarget);

        if (cTargets.Count == 0)
        {
            Logger.Warn("Reset target failed: Target candidate does not exist", "BountyHunter");
            return 0xff;
        }

        var rand = IRandom.Instance;
        var target = cTargets[rand.Next(0, cTargets.Count)];
        var targetId = target.PlayerId;
        Targets[playerId] = targetId;
        
        if (ShowTargetArrow) TargetArrow.Add(playerId, targetId);
        Logger.Info($"Change {player.GetNameWithRole()} target to: {target.GetNameWithRole()}", "BountyHunter");

        SendRPC(player.PlayerId, targetId);
        return targetId;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.AbilityButton.OverrideText(GetString("BountyHunterChangeButtonText"));
    public override void AfterMeetingTasks()
    {
        foreach (var id in playerIdList.ToArray())
        {
            if (!Main.PlayerStates[id].IsDead)
            {
                Utils.GetPlayerById(id)?.RpcResetAbilityCooldown();
                ChangeTimer[id] = 0f;
            }
        }
    }
    public static string GetTargetText(PlayerControl bounty, bool hud)
    {
        if (GameStates.IsMeeting) return "";
        var targetId = GetTarget(bounty);
        return targetId != 0xff ? $"{(hud ? GetString("BountyCurrentTarget") : GetString("Target"))}: {Main.AllPlayerNames[targetId].RemoveHtmlTags().Replace("\r\n", string.Empty)}" : "";
    }
    public static string GetTargetArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (seer == null) return "";
        if (!seer.Is(CustomRoles.BountyHunter)) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        if (!ShowTargetArrow || GameStates.IsMeeting) return "";

        //seerがtarget自身でBountyHunterのとき、
        //矢印オプションがありミーティング以外で矢印表示
        var targetId = GetTarget(seer);
        return TargetArrow.GetArrows(seer, targetId);
    }
}
