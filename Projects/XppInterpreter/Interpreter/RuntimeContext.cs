using System;
using System.Collections.Generic;
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
        public Dictionary<Parser.Data.Query, SearchInstance> Queries = new Dictionary<Parser.Data.Query, SearchInstance>();
        public Stack<IDisposable> ChangeCompanyHandlers = new Stack<IDisposable>();
        public readonly ScopeHandler ScopeHandler;
        public readonly XppProxy Proxy;
        public readonly ByteCode ByteCode;

        public bool Returned;
        public RuntimeContext InnerContext;
        public XppInterpreter Interpreter;
        public DebugAction NextAction;

        public RuntimeContext(XppProxy proxy, ByteCode bytecode)
        {
            Proxy = proxy;
            ScopeHandler = new ScopeHandler();
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
            setCounter(counter);
        }

        public void moveCounter(int offset)
        {
            if (_pc + offset < 0)
                throw new Exception("Program counter cannot be negative.");

            _pc += offset;
        }

        public void setCounter(int counter)
        {
            if (counter < 0)
                throw new Exception("Program counter cannot be negative.");

            _pc = counter;
        }
    }
}
