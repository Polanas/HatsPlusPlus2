using DuckGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatsPlusPlus; 

internal struct ProfileIdUint {
    internal uint value;
}

internal static class Ducks {
    internal static Duck MainDuck => DuckNetwork.localProfile?.duck ?? Profiles.DefaultPlayer1?.duck;

    internal static ProfileIdUint ProfileId {
        get {
            List<Profile> profiles = [
                Profiles.DefaultPlayer1,
                Profiles.DefaultPlayer2,
                Profiles.DefaultPlayer3,
                Profiles.DefaultPlayer4,
                Profiles.DefaultPlayer5,
                Profiles.DefaultPlayer6,
                Profiles.DefaultPlayer7,
                Profiles.DefaultPlayer8,
            ];
            for (uint i = 0; i < profiles.Count; i++) {
                var profile = profiles[(int)i];
                if (MainDuck.profile == profile) {
                    return new ProfileIdUint {
                        value = i + 1
                    };
                }
            }
            throw new Exception("Expected MainDuck profile to be one of the default ones");
        }
    }
}
