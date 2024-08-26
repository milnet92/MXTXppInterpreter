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

        /// <summary>
        /// Number of retries executed for this handler
        /// </summary>
        private int _retyCount = 0;

        /// <param name="start">Program counter when the handler whas created</param>
        /// <param name="offset"></param>Relative position from start where the try block ends
        /// <param name="references">Catch references</param>
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

        public void ExecuteRetry(RuntimeContext context)
        {
            ResetHandle();

            // Close anu unbalanced scope from the start to now
            context.EndScopeRange(TryPointer + 1, context.Counter);

            _retyCount ++;

            // Move to start of try block
            // + 1 because we don't one the start handle exception
            // instruction to be executed
            context.SetCounter(TryPointer + 1);
        }

        private void ResetHandle()
        {
            Exception = null;
            Catched = false;
            CanCatch = true;
        }

        internal void SetNativeRetryCount(RuntimeContext context)
        {
            // Increase retry count
            context.Proxy.Exceptions.SetRetryCount(_retyCount);
        }
    }
}
