using DuckGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatsPlusPlus; 
public static class Ducks {
    public static Duck MainDuck => DuckNetwork.localProfile?.duck ?? Profiles.DefaultPlayer1?.duck;
}
