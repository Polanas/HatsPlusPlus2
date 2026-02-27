using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Script = MoonSharp.Interpreter.Script;

#nullable enable

namespace HatsPlusPlus;

internal class LuaScript {
    internal Script value = null!;
    internal DynValue state = null!;

    internal static LuaScript New(string path) {
        var luaScript = new LuaScript();

        //HACK: !!! VERY IMPORANT !!! Ensure lua scripts are properly sandboxed. We dont want malware in our hats!
        luaScript.value = new Script(CoreModules.Preset_Complete);
        luaScript.value.Globals["PATH_DELIMETER"] = "\\";
        LuaUtils.LoadApi(luaScript);
        //TODO: what if lua script has errors?
        luaScript.state = luaScript.value.DoFile(path);
        return luaScript;
    }

    public static implicit operator Script(LuaScript input) {
        return input.value;
    }

    internal void ProtectedCall(string functionName, params object[] args) {
        try {
            var functionTable = state.Table.Get(functionName);
            if (functionTable.Function is var fn && fn is not null) {
                fn.Call(args);
            }
        } catch (ScriptRuntimeException e) {
                LuaLogger.Error($"{e.DecoratedMessage ?? e.Message}");
        }
    }

    internal void TryProtectedCall(string functionName, params object[] args) {
        try {
            var functionTable = state.Table.Get(functionName);
            if (functionTable.Function is var fn && fn is not null) {
                fn.Call(args);
            } else {
                LuaLogger.Warn($"Attempted to call a missing function {functionName}");
            }
        } catch (ScriptRuntimeException e) {
                LuaLogger.Error($"{e.DecoratedMessage ?? e.Message}");
        }
    }

    internal void SetImagesPath(string path) {
        value.Globals["imagesPath"] = path;
    }

    internal void Select() {
        ProtectedCall("select");
    }

    internal void Spawn() {
        ProtectedCall("spawn");
    }

    internal void Update(params object[] args) {
        ProtectedCall("update", args);
    }
}
