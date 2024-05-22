using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class TtsAbort : IInstruction
    {
        public string OperationCode => "TTSABORT";

        public void Execute(RuntimeContext context)
        {
            context.Proxy.Data.TtsAbort();
        }
    }
}
