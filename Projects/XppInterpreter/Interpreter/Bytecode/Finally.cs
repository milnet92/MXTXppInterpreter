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
            FinallyBlock block = context.FinallyBlocks.Pop();

            if (block.Exception != null)
            {
                if (context.ScopeHandler.AreExceptionsHandled)
                {
                    context.ScopeHandler.CurrentExceptionHandler.HandleException(block.Exception, context);
                }
                else
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(block.Exception).Throw();
                }
            }
            else if (block.Exit != null)
            {
                context.BeginExit(block.Exit);
            }
        }
    }
}
