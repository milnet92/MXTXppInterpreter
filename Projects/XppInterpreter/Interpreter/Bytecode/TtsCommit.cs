using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class TtsCommit : IInstruction
    {
        public string OperationCode => "TTSCOMMIT";

        public void Execute(RuntimeContext context)
        {
            context.Proxy.Data.TtsCommit();
        }
    }
}
