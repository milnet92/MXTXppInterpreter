using System.Collections.Generic;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class ByteCode
    {
        public List<IInstruction> Instructions;
        public List<RefFunction> DeclaredFunctions;

        public ByteCode(List<IInstruction> instructions)
        {
            Instructions = instructions;
        }
    }
}
