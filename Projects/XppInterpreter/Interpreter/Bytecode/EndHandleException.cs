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

            if (handler.NeedsToPropagateException())
            {
                if (NeedToFinalize)
                {
                    context.LastException = handler.Exception;
                }
                else
                {
                    throw handler.Exception;
                }
            }
            else if (handler.Catched)
            {
                context.LastException = null;
            }
        }
    }
}
