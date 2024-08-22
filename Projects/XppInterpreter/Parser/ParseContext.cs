using System;

namespace XppInterpreter.Parser
{
    public class ParseContext
    {
        private readonly ParseContextScope _globalScope = new ParseContextScope();

        public ParseContextScope CurrentScope { get; private set; }
        public ParseContextStack CallFunctionScope { get; } = new ParseContextStack();
        public ParseContextStack FunctionDeclarationStack { get; } = new ParseContextStack();
        public ParseContextStack<bool> LoopStack { get; } = new ParseContextStack<bool>();
        internal readonly ParseContextStack FinallyStack = new ParseContextStack();

        public ParseContext()
        {
            CurrentScope = _globalScope;
        }

        public bool CanScapeLoop()
        {
            return LoopStack.Empty || LoopStack.Peek();
        }

        public bool WithinFinally()
        {
            return !FinallyStack.Empty;
        }

        public void BeginScope()
        {
            CurrentScope = CurrentScope.Begin();
        }

        public void EndScope()
        {
            if (CurrentScope.Parent != null)
            {
                CurrentScope = CurrentScope.End();
            }
        }
    }
}
