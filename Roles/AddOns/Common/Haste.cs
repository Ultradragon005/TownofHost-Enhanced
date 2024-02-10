using System.Collections.Generic;

namespace TOHE.Roles.AddOns.Common
{
    public static class Haste
    {
        private static readonly int Id = 30000;

        public static OptionItem CanBeOnCrew;
        public static OptionItem CanBeOnImp;
        public static OptionItem CanBeOnNeutral;

        public static Dictionary<byte, bool> IsInVent;
        public static Dictionary<byte, bool> HasChanged;
        public static Dictionary<byte, bool> Activate;
        public static Dictionary<byte, float> TrueKCD;
        public static float newKCD;

        public static long NextUnix = Utils.GetTimeStamp();

        public static void SetupCustomOptions()
        {
            Options.SetupAdtRoleOptions(Id, CustomRoles.Haste, canSetNum: true, tab: TabGroup.OtherRoles);
            CanBeOnImp = BooleanOptionItem.Create(Id + 10, "ImpCanBeHaste", true, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Haste]);
            CanBeOnCrew = BooleanOptionItem.Create(Id + 11, "CrewCanBeHaste", true, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Haste]);
            CanBeOnNeutral = BooleanOptionItem.Create(Id + 12, "NeutralCanBeHaste", true, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Haste]);
        }

        public static void Init()
        {
            IsInVent = [];
            HasChanged = [];
            Activate = [];
            TrueKCD = [];
        }

        public static void Add(PlayerControl Player)
        {
            IsInVent.Add(Player.PlayerId, false);
            HasChanged.Add(Player.PlayerId, false);
            Activate.Add(Player.PlayerId, false);
        }

        public static void CoEnterVent(PlayerControl player)
        {
            IsInVent[player.PlayerId] = true;
        }
         
        public static void ExitVent(PlayerControl player)
        {
            IsInVent[player.PlayerId] = false;
        }

        public static void OnFixedUpdate(PlayerControl target)
        {
            if (!HasChanged[target.PlayerId] && Activate[target.PlayerId])
            {
                newKCD = Main.AllPlayerKillCooldown[target.PlayerId];
                TrueKCD[target.PlayerId] = Main.AllPlayerKillCooldown[target.PlayerId];
                Main.AllPlayerKillCooldown[target.PlayerId] = TrueKCD[target.PlayerId];
                target.MarkDirtySettings();
                HasChanged[target.PlayerId] = true;
            }
            
            if (HasChanged[target.PlayerId] && NextUnix + 1 <= Utils.GetTimeStamp())
            {
                Activate[target.PlayerId] = false;
                NextUnix = Utils.GetTimeStamp();
                if(!(newKCD <= 1)) newKCD --;

                if (IsInVent[target.PlayerId] && !(newKCD <= 0))
                {
                    Main.AllPlayerKillCooldown[target.PlayerId] = newKCD;
                    target.MarkDirtySettings();
                }
            }
        }

        public static void OnCheckAdittionalMurder(PlayerControl killer)
        {
            killer.ResetKillCooldown();
        }
    }
}
