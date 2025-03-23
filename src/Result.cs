namespace HatsPlusPlus;
#nullable enable

public readonly struct Result<T,E>
{
    private readonly bool isOkay;
    private readonly T? value;
    private readonly E? error;

    private Result(T? value, E? error, bool isOkay)
    {
        this.value = value;
        this.error = error;
        this.isOkay = isOkay;
    }

    public bool IsOk => isOkay;
    public bool isErr => !isOkay;

    public static Result<T,E> Okay(T value)
    {
        return new Result<T, E>(value, default, true);
    }

    public static Result<T,E> Err(E err)
    {
        return new Result<T, E>(default, err, true);
    }
}
public class TestClass
{

}
public class Test
{
    public static void Test1()
    {
        //Test2(new TestClass { });
        //Test2(null);
    }

    public static void Test2<T>(TestClass? value) 
    {

    }
}
