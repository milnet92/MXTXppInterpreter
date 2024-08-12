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
            context.ScopeHandler.AddExceptionHandler(new Core.ExceptionHandler(context.Counter, Offset, CatchReferences));
        }
    }
}
