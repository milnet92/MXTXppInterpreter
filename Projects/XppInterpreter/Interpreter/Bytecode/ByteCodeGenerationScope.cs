using System.Collections.Generic;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class ByteCodeGenerationScope
    {
        public Dictionary<string, int> Labels = new Dictionary<string, int>();
        public List<IInstruction> Instructions = new List<IInstruction>();
        public int Count => Instructions.Count;

        public void Add(IInstruction instruction)
        {
            Instructions.Add(instruction);
        }
    }
}
