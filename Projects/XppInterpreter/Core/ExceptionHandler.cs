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
        private const string CLR_ERROR_MEMBER = "CLRError";

        /// <summary>
        /// The number of open transactions when the try statement started
        /// </summary>
        private readonly int _transactionLevel;

        /// <summary>
        /// The transactions that the interpreter opened around the script
        /// </summary>
        private readonly int _globalTransactions;

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
        /// <param name="transactionLevel">The number of open transactions</param>
        /// <param name="globalTransactions">How many of them the interpreter opened around the script</param>
        public ExceptionHandler(int start, int offset, List<ExceptionCatchReference> references, int transactionLevel, int globalTransactions = 0)
        {
            TryPointer = start;
            TryEndOffset = offset;

            ExceptionCatchReferences = references;
            _transactionLevel = transactionLevel;
            _globalTransactions = globalTransactions;
        }

        public void HandleException(Exception exception, RuntimeContext context)
        {
            exception = Unwrap(exception);

            Exception = exception;

            if (CanCatch)
            {
                CanCatch = false;

                // Compiled code does this for every catch: it keeps the exception for CLRInterop::getLastException()
                context.Proxy.Exceptions.IsExceptionMember(exception, CLR_ERROR_MEMBER);

                foreach (var catchReference in ExceptionCatchReferences)
                {
                    if (_transactionLevel > _globalTransactions && !CanCatchInsideTransaction(catchReference))
                    {
                        continue;
                    }

                    bool canBeCatched = catchReference.ClrType != null
                        ? catchReference.ClrType.IsInstanceOfType(exception)
                        : CanBeCatched(exception, catchReference.ExceptionMemberName, context.Proxy.Exceptions);

                    if (canBeCatched)
                    {
                        Exception = null;
                        Catched = true;

                        int catchOffset = catchReference.Offset - (context.Counter - TryPointer);

                        RestoreTransactionLevel(context);


                        // Close any unbalanced scope
                        context.EndScopeRange(context.Counter, catchOffset);

                        if (catchReference.VariableName != null)
                        {
                            context.ScopeHandler.CurrentScope.SetVar(catchReference.VariableName, exception, context.Proxy.Casting);
                        }

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

            MoveToEnd(context);
        }

        /// <summary>
        /// A try statement inside a transaction only catches these two: every other exception goes to the code outside the transaction.
        /// </summary>
        private static bool CanCatchInsideTransaction(ExceptionCatchReference catchReference)
        {
            return string.Equals(catchReference.ExceptionMemberName, "UpdateConflict", StringComparison.OrdinalIgnoreCase)
                || string.Equals(catchReference.ExceptionMemberName, "DuplicateKeyException", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The transactions opened by the try block end. At the first level they are aborted. Inside a transaction nothing is committed yet.
        /// </summary>
        private void RestoreTransactionLevel(RuntimeContext context)
        {
            if (_transactionLevel == 0)
            {
                if (context.Proxy.Data.TtsLevel() > 0)
                {
                    context.Proxy.Data.TtsAbort();
                }

                return;
            }

            while (context.Proxy.Data.TtsLevel() > _transactionLevel)
            {
                context.Proxy.Data.TtsCommit();
            }
        }

        /// <summary>
        /// Code called through reflection throws its exception wrapped
        /// </summary>
        private static Exception Unwrap(Exception exception)
        {
            while (exception is System.Reflection.TargetInvocationException && exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            return exception;
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
            context.EndScopeRange(TryPointer + 1, context.Counter - TryPointer - 1);

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
            // Called for every instruction inside a try block: the proxy is only needed when the value changes
            if (context.NativeRetryCount != _retyCount)
            {
                context.NativeRetryCount = _retyCount;
                context.Proxy.Exceptions.SetRetryCount(_retyCount);
            }
        }

        /// <summary>
        /// Leaves the try or catch block, going to the end of the handler
        /// </summary>
        internal void MoveToEnd(RuntimeContext context)
        {
            int endOffset = TryEndOffset - (context.Counter - TryPointer);

            // Close any unbalanced scope
            context.EndScopeRange(context.Counter, endOffset);

            context.MoveCounter(endOffset);
        }
    }
}
