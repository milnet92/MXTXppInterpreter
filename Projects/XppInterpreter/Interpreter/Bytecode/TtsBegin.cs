using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class TtsBegin : IInstruction
    {
        public string OperationCode => "TTSBEGIN";

        public void Execute(RuntimeContext context)
        {
            context.Proxy.Data.TtsBegin();
        }
    }
}
