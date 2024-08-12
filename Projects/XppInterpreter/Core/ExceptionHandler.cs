using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Bytecode;

namespace XppInterpreter.Core
{
    public class ExceptionHandler
    {
        public List<ExceptionCatchReference> ExceptionCatchReferences { get; }

        /// <summary>
        /// Instruction pointer where try scope starts
        /// </summary>
        public int TryPointer { get; }

        /// <summary>
        /// Instruction pointer offset from <c>TryPointer</c> to try scope end
        /// </summary>
        public int TryEndOffset { get; }

        public Exception Exception { get; private set; }
        public bool Catched {get; private set; }
        public bool CanCatch { get; private set; } = true;

        public ExceptionHandler(int start, int offset, List<ExceptionCatchReference> references)
        {
            TryPointer = start;
            TryEndOffset = offset;

            ExceptionCatchReferences = references;
        }

        public void HandleException(Exception exception, RuntimeContext context)
        {
            Exception = exception;

            if (CanCatch)
            {
                CanCatch = false;

                foreach (var catchReference in ExceptionCatchReferences)
                {
                    if (CanBeCatched(exception, catchReference.ExceptionMemberName, context.Proxy.Exceptions))
                    {
                        Exception = null;
                        Catched = true;

                        int catchOffset = catchReference.Offset - (context.Counter - TryPointer);

                        // Close any unbalanced scope
                        context.EndScopeRange(context.Counter, catchOffset);

                        // Move to catch block
                        context.MoveCounter(catchOffset);

                        return;
                    }
                }
            }
            else
            {
                Catched = false;
            }

            int endOffset = TryEndOffset - (context.Counter - TryPointer);

            // Close any unbalanced scope
            context.EndScopeRange(context.Counter, endOffset);

            // Move to end of scope handler
            context.MoveCounter(endOffset);
        }

        private bool CanBeCatched(Exception exception, string exceptionMember, Interpreter.Proxy.IXppExceptionsProxy proxy)
        {
            return string.IsNullOrEmpty(exceptionMember) || proxy.IsExceptionMember(exception, exceptionMember);
        }

        public bool NeedsToPropagateException()
        {
            return !Catched && Exception != null;
        }
    }
}
