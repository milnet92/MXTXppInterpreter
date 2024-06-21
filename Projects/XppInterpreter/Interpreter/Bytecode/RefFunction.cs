using System.Collections.Generic;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class RefFunction
    {
        public FunctionDeclaration Declaration;
        public List<IInstruction> Instructions = new List<IInstruction>();

        public RefFunction(FunctionDeclaration declaration)
        {
            Declaration = declaration;
        }
    }
}
