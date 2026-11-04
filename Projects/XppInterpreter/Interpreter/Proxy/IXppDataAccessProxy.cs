using System;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppDataAccessProxy
    {
        void TtsBegin();
        void TtsCommit();
        void TtsAbort();

        /// <summary>
        /// The number of open transactions: <c>appl.ttsLevel()</c>
        /// </summary>
        int TtsLevel();
        IDisposable CreateChangeCompanyHandler(string datAreaId);
        IDisposable CreateUncheckedHandler(int uncheckedValue, string className, string methodName);
        void Next(object common);
        void Flush(string tableName);
    }
}
