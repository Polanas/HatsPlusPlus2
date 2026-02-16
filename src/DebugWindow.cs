using DuckGame;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatsPlusPlus; 

internal static class HppDebugWindow {
    internal static bool isOpen;

    internal static void Init() {
        DevConsole.AddCommand(new CMD("hpp_debug_win", () => {
            isOpen = true;
        }));
    }

    internal static void Draw() {
        if (!isOpen) {
            return;
        }

        ImGui.SetWindowSize(new System.Numerics.Vector2(200, 200), ImGuiCond.Once);
        ImGui.Begin("Hats++ Debug Window", ref isOpen);
        var mainDuck = Ducks.mainDuck;
        if (mainDuck != null) {
            ImGui.Text($"main duck position: {Ducks.mainDuck.position}");
            ImGui.Text($"main duck hat position: {Ducks.mainDuck.hat.position}");
            ImGui.Text($"offset: {mainDuck.hat.position - mainDuck.position}");
        }
        ImGui.End();
    }
}
