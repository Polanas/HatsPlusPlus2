using System;
using System.Collections.Generic;

namespace HatsPlusPlus; 

public static class BoolExtensions {
    /// <summary>
    /// Returns Some(t) if the bool is true, or None otherwise.
    /// </summary>
    public static Option<T> Then<T>(this bool boolean, T t) {
        if (boolean) {
            return t;
        }
        return None;
    }


    /// <summary>
    /// Returns Some(func()) if the bool is true, or None otherwise.
    /// </summary>
    public static Option<T> ThenSome<T>(this bool boolean, Func<T> func) {
        if (boolean) {
            return func();
        }
        return None;
    }
}

public static class ListExtensions {
    public static Option<T> Get<T>(this List<T> list, int index) {
        return (index < list.Count).ThenSome(() => list[index]);
    }
    public static T RemoveAndGet<T>(this IList<T> list, int index) {
        T value = list[index];
        list.RemoveAt(index);
        return value;
    }
}

public static class ArrayExtensions {
    public static Option<T> Get<T>(this T[] array, int index) {
        return (index < array.Length).ThenSome(() => array[index]);
    }
}

public static class OptionExtensions {
    public static Option<U> AndThen<T, U>(this Option<T> self, Func<T, Option<U>> func) {
        return self.Match(
            (value) => func(value),
            () => None
        );
    }

    public static T ValueOr<T>(this Option<T> self, T value) {
        return self.Match(
            (value) => value,
            () => value);
    }

    public static T ValueOrUnsafe<T>(this Option<T> self, T value) {
        return self.MatchUnsafe(
            (value) => value,
            () => value);
    }

    public static T ValueOrElse<T>(this Option<T> self, Func<T> func) {
        return self.Match(
            (value) => value,
            () => func());
    }

    public static T ValueOrElseUnsafe<T>(this Option<T> self, Func<T> func) {
        return self.MatchUnsafe(
            (value) => value,
            () => func());
    }
}

public static class DictionaryExt {
    public static Option<TValue> RemoveGet<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key) {
        self.TryGetValue(key, out var value);
        var isRemoved = self.Remove(key);
        if (isRemoved) {
            return value;
        } else {
            return None;
        }
    }

    public static Option<V> Get<K,V>(this Dictionary<K,V> dict, K key) {
        if (dict.TryGetValue(key, out var value)) {
            return value;
        }
        return None;
    }
}

public class CalledUnwrapOnNoneValueException : Exception { }
public class CalledUnwrapOkOnErr : Exception {
    public string message;
    public CalledUnwrapOkOnErr(string message) : base(message)
    {
        this.message = message;
    }
}

public class CalledUnwrapErrOnOk() : Exception { }

public static class Extensions {
    public static T Unwrap<T>(this Option<T> option) {
        return option.IfNone(() => throw new CalledUnwrapOnNoneValueException());
    }

    public static L UnwrapOk<L>(this Either<L,string> either)
    {
        return either.IfRight((err) => throw new CalledUnwrapOkOnErr(err));
    }

    public static string UnwrapErr<L>(this Either<L,string> either)
    {
        return either.IfLeft((_) => throw new CalledUnwrapErrOnOk());
    }
}
