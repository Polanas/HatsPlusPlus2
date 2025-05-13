using System;
using System.Collections.Generic;

namespace HatsPlusPlus; 

internal static class BoolExtensions {
    /// <summary>
    /// Returns Some(t) if the bool is true, or None otherwise.
    /// </summary>
    internal static Option<T> Then<T>(this bool boolean, T t) {
        if (boolean) {
            return t;
        }
        return None;
    }


    /// <summary>
    /// Returns Some(func()) if the bool is true, or None otherwise.
    /// </summary>
    internal static Option<T> ThenSome<T>(this bool boolean, Func<T> func) {
        if (boolean) {
            return func();
        }
        return None;
    }
}

internal static class ListExtensions {
    internal static Option<T> Get<T>(this List<T> list, int index) {
        return (index < list.Count).ThenSome(() => list[index]);
    }
    internal static T RemoveAndGet<T>(this IList<T> list, int index) {
        T value = list[index];
        list.RemoveAt(index);
        return value;
    }
}

internal static class ArrayExtensions {
    internal static Option<T> Get<T>(this T[] array, int index) {
        return (index < array.Length).ThenSome(() => array[index]);
    }
}

internal static class OptionExtensions {
    internal static Option<U> AndThen<T, U>(this Option<T> self, Func<T, Option<U>> func) {
        return self.Match(
            (value) => func(value),
            () => None
        );
    }

    internal static T ValueOr<T>(this Option<T> self, T value) {
        return self.Match(
            (value) => value,
            () => value);
    }

    internal static T ValueOrUnsafe<T>(this Option<T> self, T value) {
        return self.MatchUnsafe(
            (value) => value,
            () => value);
    }

    internal static T ValueOrElse<T>(this Option<T> self, Func<T> func) {
        return self.Match(
            (value) => value,
            () => func());
    }

    internal static T ValueOrElseUnsafe<T>(this Option<T> self, Func<T> func) {
        return self.MatchUnsafe(
            (value) => value,
            () => func());
    }
}

internal static class DictionaryExt {
    internal static Option<TValue> RemoveGet<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key) {
        self.TryGetValue(key, out var value);
        var isRemoved = self.Remove(key);
        if (isRemoved) {
            return value;
        } else {
            return None;
        }
    }

    internal static Option<V> Get<K,V>(this Dictionary<K,V> dict, K key) {
        if (dict.TryGetValue(key, out var value)) {
            return value;
        }
        return None;
    }
}

internal class CalledUnwrapOnNoneValueException : Exception { }
internal class CalledUnwrapOkOnErr : Exception {
    internal string message;
    internal CalledUnwrapOkOnErr(string message) : base(message)
    {
        this.message = message;
    }
}

internal class CalledUnwrapErrOnOk() : Exception { }

internal static class Extensions {
    internal static T Unwrap<T>(this Option<T> option) {
        return option.IfNone(() => throw new CalledUnwrapOnNoneValueException());
    }

    internal static T Expect<T>(this Option<T> option, string message) {
        return option.IfNone(() => throw new Exception(message));
    }

    internal static L UnwrapOk<L>(this Either<L,string> either)
    {
        return either.IfRight((err) => throw new CalledUnwrapOkOnErr(err));
    }

    internal static string UnwrapErr<L>(this Either<L,string> either)
    {
        return either.IfLeft((_) => throw new CalledUnwrapErrOnOk());
    }
}
