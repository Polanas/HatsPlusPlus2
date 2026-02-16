using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatsPlusPlus;


internal static class LuaLogger {
    static List<string> logs = new();
    const int MAX_LOGS_AMOUNT = 1000;

    internal static void Warn(string message) {
        Log($"[WARN] {message}");
    }

    internal static void Info(string message) {
        Log($"[INFO] {message}");
    }

    internal static void Error(string message) {
        Log($"[ERROR] {message}");
    }

    internal static void Log(string message) {
        logs.Add(message);

        if (logs.Count > MAX_LOGS_AMOUNT) {
            logs.RemoveAt(0);
        }
    }

    internal static void Show() {
        ImGui.Begin("Logs");
        foreach (var log in logs) {
            ImGui.TextWrapped(log);
        }
        ImGui.End();
    }
}
