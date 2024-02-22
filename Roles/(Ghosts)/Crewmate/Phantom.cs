using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TOHE.Options;

namespace TOHE.Roles.Ghosts.Crewmate;

public static class Phantom
{
    private static readonly int Id = 14900;

    public static OptionItem PhantomCanVent;
    public static OptionItem PhantomSnatchesWin;
    public static OptionItem PhantomCanGuess;
    public static OverrideTasksData PhantomTasks;
    public static bool AttemptedAssign;
    public static void SetupCustomOptions()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Phantom);
        PhantomSnatchesWin = BooleanOptionItem.Create(Id + 10, "SnatchesWin", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
        PhantomTasks = OverrideTasksData.Create(Id + 11, TabGroup.CrewmateRoles, CustomRoles.Phantom);
    }

    public static void Init()
    {
        AttemptedAssign = false;
    }
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    public static (bool, int, int) TaskData => (PhantomTasks.assignCommonTasks.GetBool(), PhantomTasks.numLongTasks.GetInt(), PhantomTasks.numShortTasks.GetInt());
    public static bool IsAssignTarget(PlayerControl pc) => pc.GetCustomRole() is not CustomRoles.Lazy and not CustomRoles.Needy && AttemptedAssign == false;

    public static bool OnAssign(PlayerControl pc)
    {
        if (!IsAssignTarget(pc)) return false;
        AttemptedAssign = true;

        pc.RpcSetCustomRole(CustomRoles.Workhorse);
        var taskState = pc.GetPlayerTaskState();
        taskState.AllTasksCount += NumLongTasks + NumShortTasks;
        taskState.CompletedTasksCount++; //今回の完了分加算

        if (AmongUsClient.Instance.AmHost)
        {
            GameData.Instance.RpcSetTasks(pc.PlayerId, Array.Empty<byte>()); //タスクを再配布
            pc.SyncSettings();
            Utils.NotifyRoles(SpecifySeer: pc);
        }

        return true;
    }
}
