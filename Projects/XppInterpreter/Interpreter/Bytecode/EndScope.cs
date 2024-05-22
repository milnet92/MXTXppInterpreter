using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class EndScope : IInstruction
    {
        public string OperationCode => "END_SCOPE";

        public void Execute(RuntimeContext context)
        {
            context.ScopeHandler.EndScope();
        }
    }
}
