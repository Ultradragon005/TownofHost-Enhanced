﻿using TOHE;
using static TOHE.Options;
using System.Collections.Generic;

namespace TOHE.Roles.Crewmate
{
    internal class Dictator : RoleBase
    {
        public const int Id = 11600;
        public bool On = false;
        public override bool IsEnable => On;
        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

        public static void SetupCustomOptions()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Dictator);
        }

        public override void Init()
        {
            On = false;
        }
        public override void Add(byte playerId)
        {
            On = true;
        }

    }
}
