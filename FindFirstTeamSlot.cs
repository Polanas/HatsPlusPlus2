using Coroutines;
using DuckGame;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatsPlusPlus;
public static class FindFirstTeamSlot {
    public static IEnumerator Find() {
        var emptyTeam = TeamsStorage.BitmapToTeam(Bitmap.FromPath(Mod.GetPath<HatsPlusPlus2>("empty.png")), "empty").UnwrapOk();
        Teams.AddExtraTeam(emptyTeam);
        var customTeamIndex = (ushort)Teams.core.extraTeams.IndexOf(emptyTeam);
        Send.Message(new NMSpecialHat(emptyTeam, DuckNetwork.localProfile, false));
        yield return null;
    }
}
