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
            // A using or a changecompany inside the catch block is closed first
            int handlers = context.ScopeHandler.HandlersInsideCatchBlock;

            if (handlers > 0)
            {
                context.BeginExit(PendingExit.Retry(handlers));
            }
            else
            {
                context.ScopeHandler.CurrentExceptionHandler.ExecuteRetry(context);
            }
        }
    }
}
