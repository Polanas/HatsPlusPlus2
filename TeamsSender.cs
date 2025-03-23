using Coroutines;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatsPlusPlus;
public static class TeamsSender {
    static CoroutineRunner coroutines;

    public static void Init() {
        coroutines = new CoroutineRunner();
    }

    public static void Update(GameTime gameTime) {

    }
}
