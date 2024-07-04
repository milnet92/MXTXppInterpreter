using System;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppDataAccessProxy
    {
        void TtsBegin();
        void TtsCommit();
        void TtsAbort();
        IDisposable CreateChangeCompanyHandler(string datAreaId);
        void Next(object common);
    }
}
