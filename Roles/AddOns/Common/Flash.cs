using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Linq.Expressions.Interpreter;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Flash
{
    private static readonly int Id = 26100;
    

    private static OptionItem OptionSpeed;
    public static OptionItem FlashLabel;

    public static readonly string[] flashLabel =
[
    "Flash",
    "Slow",
];

    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Flash, canSetNum: true, tab: TabGroup.Addons);
        FlashLabel = StringOptionItem.Create(Id + 10, "FlashLabel", flashLabel, 0, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Flash]);
        OptionSpeed = FloatOptionItem.Create(Id + 11, "FlashSpeed", new(0.25f, 5f, 0.25f), 2.5f, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Flash])
            .SetValueFormat(OptionFormat.Multiplier);

        Utils.RunLabelTRUE("Flash");
    }
    public static void SetSpeed(byte playerId, bool clearAddOn)
    {
        if (!clearAddOn)
            Main.AllPlayerSpeed[playerId] = OptionSpeed.GetFloat();
        else
            Main.AllPlayerSpeed[playerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
    }
}