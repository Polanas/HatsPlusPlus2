using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatsPlusPlus.AssertFn;

internal static class Replude {
    internal static void Assert(bool assertion, string message) {
        if (!assertion) {
            throw new Exception($"Assertion failed with message: {message}");
        }
    }
}
