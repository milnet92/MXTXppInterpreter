using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class BeginHandleException : IInstruction
    {
        public string OperationCode => $"BEGIN_HEXCEPTION {Offset}";
        public int Offset { get; }
        public List<ExceptionCatchReference> CatchReferences = new List<ExceptionCatchReference>();

        public BeginHandleException(int offset)
        {
            Offset = offset;
        }

        public void Execute(RuntimeContext context)
        {
            // The option that rolls everything back opens a transaction around the script: the script doesn't count it
            int globalTransactions = context.Interpreter?.Options?.EmitGlobalTtsAbort == true ? 1 : 0;

            context.ScopeHandler.AddExceptionHandler(new Core.ExceptionHandler(context.Counter, Offset, CatchReferences, context.Proxy.Data.TtsLevel(), globalTransactions));
        }
    }
}
