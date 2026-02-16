using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using DuckGame;

namespace HatsPlusPlus
{
    [HarmonyPatch(typeof(HatSelector))]
    [HarmonyPatch("AllTeams")]
    internal class HatSelectorPatched
    {
        [HarmonyPrefix]
        private static bool AllTeams(ref List<Team> __result, Profile ____profile)
        {
            if (!Network.isActive)
            {
                __result = Teams.all;
                return false;
            }
            if (____profile == null)
            {
                __result = Teams.core.teams;
                return false;
            }
            if (____profile.connection != DuckNetwork.localConnection)
            {
                List<Team> teams = new List<Team>(Teams.core.teams);
                foreach (Team t in ____profile.customTeams)
                {
                    if (t.name.Contains(Constants.TEAM_TAG))
                        continue;

                    teams.Add(t);
                }
                __result = teams;
                return false;
            }
            List<Team> teams2 = new List<Team>(Teams.core.teams);
            foreach (Team t2 in Teams.core.extraTeams)
            {
                if (t2.name.Contains(Constants.TEAM_TAG))
                    continue;

                teams2.Add(t2);
            }
            __result = teams2;
            return false;
        }
    }
}
