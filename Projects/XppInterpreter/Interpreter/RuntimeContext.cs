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
        public Dictionary<Parser.Data.Query, ISearchInstance> Queries = new Dictionary<Parser.Data.Query, ISearchInstance>();
        public Stack<IDisposable> ChangeCompanyHandlers = new Stack<IDisposable>();
        public readonly ScopeHandler ScopeHandler;
        public readonly XppProxy Proxy;
        public readonly ByteCode ByteCode;
        public Exception LastException;

        public bool Returned;
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

        public void EndScopeRange(int start, int end)
        {
            var rangeToCheck = ByteCode.Instructions.GetRange(start, end);

            // Count the number of scope instructions
            int beginScopeCount = rangeToCheck.Count(instruction => instruction is BeginScope);
            int endScopeCount = rangeToCheck.Count(instruction => instruction is EndScope);

            // The number of scopes to close is the number of orphane begin scope instructions
            ScopeHandler.EndScope(Math.Abs(endScopeCount - beginScopeCount));
        }

    }
}
