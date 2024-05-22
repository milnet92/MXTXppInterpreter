using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class ByteCode
    {
        public List<IInstruction> Instructions;

        public ByteCode(List<IInstruction> instructions)
        {
            Instructions = instructions;
        }
    }
}
