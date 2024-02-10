using MS.Internal.Xml.XPath;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.AddOns.Common
{
    public static class Haste
    {
        private static readonly int Id = 28000; // temporary id, there's alot of merges right now

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
            if (!Main.AllPlayerKillCooldown.ContainsKey(target.PlayerId)) return;
            if (!TrueKCD.ContainsKey(target.PlayerId)) TrueKCD.Add(target.PlayerId, Main.AllPlayerKillCooldown[target.PlayerId]);

            if (!HasChanged[target.PlayerId] && Activate[target.PlayerId])
            {
                newKCD = TrueKCD[target.PlayerId];
                HasChanged[target.PlayerId] = true;
                //Main.AllPlayerKillCooldown[target.PlayerId] = TrueKCD[target.PlayerId];
                //target.MarkDirtySettings();
            }
            
            if (HasChanged[target.PlayerId] && NextUnix + 1 <= Utils.GetTimeStamp())
            {
                
                Main.AllPlayerKillCooldown[target.PlayerId] = TrueKCD[target.PlayerId];
                target.MarkDirtySettings(); // how the fuck is this the fix, IT ALSO SOMEFUCKING HOW gets true shading, nah I'm dead..
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
            Main.AllPlayerKillCooldown[killer.PlayerId] = TrueKCD[killer.PlayerId];
            killer.ResetKillCooldown();
        }
    }
}
