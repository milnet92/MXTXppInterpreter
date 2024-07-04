namespace XppInterpreter.Interpreter.Proxy
{
    public class XppProxy
    {
        public readonly IIntrinsicFunctionProvider Intrinsic;
        public readonly IXppBinaryOperationProxy Binary;
        public readonly IXppCastingProxy Casting;
        public readonly IXppReflectionProxy Reflection;
        public readonly IXppUnaryOperationProxy Unary;
        public readonly IXppDataAccessProxy Data;
        public readonly IXppExceptionsProxy Exceptions;
        public readonly IXppQueryGenerationProxy QueryGeneration;

        public XppProxy(
            IIntrinsicFunctionProvider intrinsic,
            IXppBinaryOperationProxy binary,
            IXppCastingProxy casting,
            IXppUnaryOperationProxy unary,
            IXppDataAccessProxy data,
            IXppReflectionProxy reflection,
            IXppExceptionsProxy exceptions,
            IXppQueryGenerationProxy queryGeneration)
        {
            Intrinsic = intrinsic;
            Binary = binary;
            Casting = casting;
            Unary = unary;
            Data = data;
            Reflection = reflection;
            Exceptions = exceptions;
            QueryGeneration = queryGeneration;
        }
    }
}
