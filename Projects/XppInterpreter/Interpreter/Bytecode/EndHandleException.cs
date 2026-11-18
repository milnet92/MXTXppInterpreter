using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Core;

namespace XppInterpreter.Interpreter.Bytecode
{
    class EndHandleException : IInstruction
    {
        public string OperationCode => "END_HEXCEPTION";
        public bool NeedToFinalize { get; }

        public EndHandleException(bool needToFinalize)
        {
            NeedToFinalize = needToFinalize;
        }

        public void Execute(RuntimeContext context)
        {
            ExceptionHandler handler = context.ScopeHandler.RemoveExceptionHandler();
            Exception exception = handler.NeedsToPropagateException() ? handler.Exception : null;
            PendingExit exit = context.PendingExit;

            context.PendingExit = null;

            if (NeedToFinalize)
            {
                context.FinallyBlocks.Push(new FinallyBlock(exception, exit, context.ScopeHandler.ExceptionHandlerCount));
            }
            else if (exception != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
            }
            else if (exit != null)
            {
                context.BeginExit(exit);
            }
        }
    }
}
