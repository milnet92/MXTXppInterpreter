using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public XppProxy(
            IIntrinsicFunctionProvider intrinsic,
            IXppBinaryOperationProxy binary,
            IXppCastingProxy casting,
            IXppUnaryOperationProxy unary,
            IXppDataAccessProxy data,
            IXppReflectionProxy reflection,
            IXppExceptionsProxy exceptions)
        {
            Intrinsic = intrinsic;
            Binary = binary;
            Casting = casting;
            Unary = unary;
            Data = data;
            Reflection = reflection;
            Exceptions = exceptions;
        }
    }
}
