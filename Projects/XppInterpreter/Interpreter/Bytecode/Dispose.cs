using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Dispose : IInstruction
    {
        public string VariableName { get; }
        public string OperationCode => $"DISPOSE {VariableName}";

        public Dispose(string variableName)
        {
            VariableName = variableName;
        }

        public void Execute(RuntimeContext context)
        {
            (context.ScopeHandler.GetVar(VariableName) as System.IDisposable)?.Dispose();
        }
    }
}
