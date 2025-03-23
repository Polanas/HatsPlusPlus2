using System;

namespace HatsPlusPlus; 

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
