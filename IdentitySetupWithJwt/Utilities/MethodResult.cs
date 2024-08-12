namespace IdentitySetupWithJwt.Utilities;

public abstract record MethodResult<T>
{
    private MethodResult() { }
    public abstract TOut Match<TOut>(Func<string, TOut> whenLeft, Func<T, TOut> whenRight);

    public record Success(T Data) : MethodResult<T>
    {
        public override TOut Match<TOut>(Func<string, TOut> whenLeft, Func<T, TOut> whenRight) => whenRight(Data);
    }

    public record Failure(string ErrorMessage) : MethodResult<T>
    {
        public override TOut Match<TOut>(Func<string, TOut> whenLeft, Func<T, TOut> whenRight) => whenLeft(ErrorMessage);
    }
}