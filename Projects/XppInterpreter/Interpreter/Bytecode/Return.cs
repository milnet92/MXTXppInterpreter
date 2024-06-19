using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Return : IInstruction
    {
        public string OperationCode => "RETURN";

        public Return() { }

        public void Execute(RuntimeContext context)
        {
            context.Returned = true;
        }
    }
}
