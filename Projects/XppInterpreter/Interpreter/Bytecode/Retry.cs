using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class Retry : IInstruction
    {
        public string OperationCode => "RETRY";

        public void Execute(RuntimeContext context)
        {
            throw new NotImplementedException("Retry statement is not implemented.");
        }
    }
}
