using System;
using System.Collections.Generic;
using System.Linq;
using XppInterpreter.Core;
using XppInterpreter.Interpreter.Bytecode;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Interpreter.Query;

namespace XppInterpreter.Interpreter
{
    public class RuntimeContext
    {
        private int _pc = 0;
        public int Counter => _pc;
        public Stack<object> Stack => ScopeHandler.CurrentScope.Stack;

        // Created when they are first used. Every call of a local function, and every value of a where clause that needs
        // a program, runs in a context of its own, and most of them never select, dispose or leave through a finally block.
        private Dictionary<Parser.Data.Query, ISearchInstance> _queries;
        private Stack<IDisposable> _disposables;
        private System.Runtime.CompilerServices.ConditionalWeakTable<object, ISearchInstance> _pendingFetches;
        private Stack<FinallyBlock> _finallyBlocks;

        public Dictionary<Parser.Data.Query, ISearchInstance> Queries => _queries ?? (_queries = new Dictionary<Parser.Data.Query, ISearchInstance>());
        public Stack<IDisposable> Disposables => _disposables ?? (_disposables = new Stack<IDisposable>());

        internal System.Runtime.CompilerServices.ConditionalWeakTable<object, ISearchInstance> PendingFetches =>
            _pendingFetches ?? (_pendingFetches = new System.Runtime.CompilerServices.ConditionalWeakTable<object, ISearchInstance>());

        internal Stack<FinallyBlock> FinallyBlocks => _finallyBlocks ?? (_finallyBlocks = new Stack<FinallyBlock>());

        public readonly ScopeHandler ScopeHandler;
        public readonly XppProxy Proxy;
        public readonly ByteCode ByteCode;

        public bool Returned;
        public object ReturnValue;
        internal PendingExit PendingExit;
        internal int NativeRetryCount = -1;
        public RuntimeContext InnerContext;
        public XppInterpreter Interpreter;
        public DebugAction NextAction;

        public RuntimeContext(XppProxy proxy, ByteCode bytecode)
        {
            Proxy = proxy;
            ScopeHandler = new ScopeHandler(proxy);
            ByteCode = bytecode;
        }

        public RuntimeContext(XppProxy proxy, ByteCode bytecode, ScopeHandler scopeHandler)
        {
            Proxy = proxy;
            ScopeHandler = scopeHandler;
            ByteCode = bytecode;
        }

        public RuntimeContext(XppProxy proxy, ByteCode bytecode, ScopeHandler scopeHandler, int counter) : this(proxy, bytecode, scopeHandler)
        {
            SetCounter(counter);
        }

        public void MoveCounter(int offset)
        {
            if (_pc + offset < 0)
            {
                throw new Exception("Program counter cannot be negative.");
            }

            _pc += offset;
        }

        public void SetCounter(int counter)
        {
            if (counter < 0)
            {
                throw new Exception("Program counter cannot be negative.");
            }

            _pc = counter;
        }

        internal void BeginExit(PendingExit exit)
        {
            PendingExit = exit;
            ContinueExit();
        }

        /// <summary>
        /// Goes to the finally block of the next try statement, or finishes the exit when there are no more.
        /// </summary>
        internal void ContinueExit()
        {
            var exit = PendingExit;

            if (exit.HandlersToLeave > 0)
            {
                exit.HandlersToLeave--;
                ScopeHandler.CurrentExceptionHandler.MoveToEnd(this);
                return;
            }

            PendingExit = null;

            if (exit.IsReturn)
            {
                Returned = true;
            }
            else if (exit.IsRetry)
            {
                ScopeHandler.CurrentExceptionHandler.ExecuteRetry(this);
            }
            else
            {
                int offset = exit.TargetCounter - Counter;

                EndScopeRange(offset < 0 ? exit.TargetCounter : Counter, Math.Abs(offset));
                SetCounter(exit.TargetCounter);
            }
        }

        /// <summary>
        /// An exception that leaves a finally block replaces what the block was holding
        /// </summary>
        internal void AbandonFinallyBlocks(int handlerCount)
        {
            while (_finallyBlocks != null && _finallyBlocks.Count > 0 && _finallyBlocks.Peek().HandlerCount >= handlerCount)
            {
                _finallyBlocks.Pop();
            }
        }

        /// <param name="start">The first instruction of the range</param>
        /// <param name="count">How many instructions it has</param>
        public void EndScopeRange(int start, int count)
        {
            var instructions = ByteCode.Instructions;

            if (start < 0 || count < 0 || start + count > instructions.Count)
            {
                throw new ArgumentException("The range is outside the instructions of the program.");
            }

            // Count the number of scope instructions. Every break and continue comes here, so without a copy of the range.
            int beginScopeCount = 0, endScopeCount = 0;

            for (int i = start; i < start + count; i++)
            {
                if (instructions[i] is BeginScope)
                {
                    beginScopeCount++;
                }
                else if (instructions[i] is EndScope)
                {
                    endScopeCount++;
                }
            }

            // The number of scopes to close is the number of orphane begin scope instructions
            ScopeHandler.EndScope(Math.Abs(endScopeCount - beginScopeCount));
        }

    }
}
