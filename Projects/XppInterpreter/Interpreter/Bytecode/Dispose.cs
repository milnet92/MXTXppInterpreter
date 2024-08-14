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
            object disposable = context.ScopeHandler.GetVar(VariableName);

            if (disposable != null)
            {
                var disposeMethodName = Core.ReflectionHelper.GetMethodInvariantName(disposable.GetType(), "Dispose");
                System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(disposeMethodName));
                disposable.GetType().GetMethod(disposeMethodName).Invoke(disposable, null);
            }
        }
    }
}
