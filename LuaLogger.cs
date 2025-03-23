using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatsPlusPlus;


public static class LuaLogger {
    static List<string> logs = new();
    const int MAX_LOGS_AMOUNT = 1000;

    public static void Log(string message) {
        logs.Add(message);

        if (logs.Count > MAX_LOGS_AMOUNT) {
            logs.RemoveAt(0);
        }
    }

    public static void Show() {
        ImGui.Begin("Logs");
        foreach (var log in logs) {
            ImGui.Text(log);
        }
        ImGui.End();
    }
}
