using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Finally : IInstruction
    {
        public string OperationCode => "END_FINALLY";

        public void Execute(RuntimeContext context)
        {
            if (context.LastException != null)
            {
                if (context.ScopeHandler.AreExceptionsHandled)
                {
                    context.ScopeHandler.CurrentExceptionHandler.HandleException(context.LastException, context);
                }
                else
                { 
                    throw context.LastException;
                }
            }
        }
    }
}
