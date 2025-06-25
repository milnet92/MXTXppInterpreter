using System;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppDataAccessProxy
    {
        void TtsBegin();
        void TtsCommit();
        void TtsAbort();
        IDisposable CreateChangeCompanyHandler(string datAreaId);
        IDisposable CreateUncheckedHandler(int uncheckedValue, string className, string methodName);
        void Next(object common);
    }
}
