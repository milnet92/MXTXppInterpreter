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
            var exception = context.LastException;

            if (exception != null)
            {
                // It's on its way out: a finally block inside the catch that handles it must not see it
                context.LastException = null;

                if (context.ScopeHandler.AreExceptionsHandled)
                {
                    context.ScopeHandler.CurrentExceptionHandler.HandleException(exception, context);
                }
                else
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
                }
            }
            else if (context.PendingExit != null)
            {
                context.ContinueExit();
            }
        }
    }
}
