using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatsPlusPlus; 
internal static class ScriptExtensions {
    internal static DynValue FixedCall(this Script script, object function, params object[] args) {
        var value = script.Call(function, args);
        if (value.Type == DataType.TailCallRequest) {
            return script.Call(value.TailCallData.Function, args);
        }
        return value;
    }
}
