using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class BeginScope : IInstruction
    {
        public string OperationCode => "BEGIN_SCOPE";

        public void Execute(RuntimeContext context)
        {
            context.ScopeHandler.BeginScope();
        }
    }
}
