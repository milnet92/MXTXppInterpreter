using System;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppExceptionsProxy
    {
        void Throw(object obj);
        bool IsExceptionMember(Exception exception, string exceptionMember);

    }
}
