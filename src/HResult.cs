using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;

namespace HatsPlusPlus.Result;
#nullable enable

internal static class Prelude {
    [System.Diagnostics.Contracts.Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static HResult<T> Ok<T>(T value)
    {
        Assert(value != null, "expected Ok Result variant to not be null");
        return HResult<T>.OkUnsafe(value);
    }

    [System.Diagnostics.Contracts.Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static HResult<T> Err<T>(string error)
    {
        Assert(error != null, "expected Err Result variant to not be null");
        return HResult<T>.ErrUnsafe(error!);
    }
}

internal enum ResultState {
    Ok,
    Err
}

internal struct HResult<T>
{
    private ResultState state;
    private T? value;
    private string? error;
    private List<string> context;

    public static implicit operator HResult<T>(string err) {
        return ErrUnsafe(err);
    }

    public static implicit operator HResult<T>(T value) {
        return OkUnsafe(value);
    }

    internal static HResult<T> OkUnsafe(T value) {
        return new HResult<T> {
            value = value,
            error = null,
            state = ResultState.Ok,
            context = [],
        };
    }

    internal static HResult<T> ErrUnsafe(string error) {
        return new HResult<T> {
            error = error,
            value = default(T),
            state = ResultState.Err,
            context = [],
        };
    }

    internal T OkUnsafe() {
        return value!;
    }

    internal string ErrUnsafe() {
        return error!;
    }

    internal (T,string) OkErrUnsafe() {
        return (value!, error!);
    }

    internal HResult<T> WithContext(string context) {
        if (this.context == null) {
            this.context = [];
        }

        this.context.Add(context);

        return this;
    }

    internal R MatchRet<R>(Func<T, R> Ok, Func<string, R> Err) {
        return IsOk ? Ok(value!) : Err(error!);
    }

    internal void Match(Action<T> Ok, Action<string> Err) {
        if (IsOk) {
            Ok(value!);
        } else {
            Err(error!);
        }
    }

    public override string ToString() {
        var self = this;
        return MatchRet(
            (value) => value!.ToString(),
            (error) => {
                string context = "";
                if (context != null) {
                    context = "Context:";
                    foreach (var current in self.context) {
                        context += $"\n{current}";
                    }
                }
                if (context == "") {
                    return error;
                }
                return $"{error}\n{context}";
            });
    }

    internal T Unwrap() {
        var self = this;
        return MatchRet(
            (value) => value,
            (err) => throw new Exception($"Called Unwrap on an Err value: {self.ToString()}")
        );
    }

    internal string UnwrapErr() {
        var self = this;
        return MatchRet(
            (value) => throw new Exception($"Called UnwrapErr on an Ok value"),
            (err) => err
        );
    }

    internal T UnwrapOr(T value) {
        var self = this;
        return MatchRet(
            (value) => value,
            (err) => value
        );
    }

    internal T UnwrapOrElse(Func<T> valueFn) {
        var self = this;
        return MatchRet(
            (value) => value,
            (err) => valueFn()
        );
    }

    internal T Expect(string message) {
        var self = this;
        return MatchRet(
            (value) => value,
            (err) => throw new Exception($"{message}\n {self.ToString()}")
        );
    }

    internal HResult<U> Map<U>(Func<T, U> map) {
        var self = this;
        return MatchRet(
            (value) => HResult<U>.OkUnsafe(map(value)),
            (err) => HResult<U>.ErrUnsafe(err)
        );
    }

    internal HResult<U> AndThen<U>(Func<T, HResult<U>> op) {
        var self = this;
        return MatchRet(
            (value) => op(value),
            (err) => HResult<U>.ErrUnsafe(err)
        );
    }

    internal U MapOr<U>(U defaultValue, Func<T, U> map) {
        return MatchRet(
            (value) => map(value),
            (err) => defaultValue
        );
    }

    internal HResult<T> MapErr(Func<string, string> mapErr) {
        var self = this;
        return MatchRet(
            (value) => self,
            (err) => ErrUnsafe(mapErr(err))
        );
    }

    internal U MapOrElse<U>(Func<string, U> defaultFn, Func<T, U> map) {
        var self = this;
        return MatchRet(
            (value) => map(value),
            (err) => defaultFn(err)
        );
    }

    internal bool IsOk => state == ResultState.Ok;
    internal bool isErr => state == ResultState.Err;
}
