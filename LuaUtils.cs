using DuckGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Script = MoonSharp.Interpreter.Script;

namespace HatsPlusPlus;
public static class LuaUtils {
    public static void LoadApi(Script scipt) {
        scipt.Globals["mousePos"] = () => {
            return new Vec2(10, 10);
        };
    }
}
