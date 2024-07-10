using System.Collections.Generic;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class ByteCode
    {
        public List<IInstruction> Instructions;
        public List<RefFunction> DeclaredFunctions;

        internal ByteCode()
        {
            Instructions = new List<IInstruction>();
            DeclaredFunctions = new List<RefFunction>();
        }

        public ByteCode(List<IInstruction> instructions)
        {
            Instructions = instructions;
        }
    }
}
