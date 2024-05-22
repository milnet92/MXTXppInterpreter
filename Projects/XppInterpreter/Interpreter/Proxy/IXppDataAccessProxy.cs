using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppDataAccessProxy
    {
        void TtsBegin();
        void TtsCommit();
        void TtsAbort();
        IDisposable CreateChangeCompanyHandler(string datAreaId);
    }
}
