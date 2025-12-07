using System;
using XppInterpreter.Core;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Lexer;

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
        internal readonly ParseContextStack CatchStack = new ParseContextStack();

        public ParseContext()
        {
            CurrentScope = _globalScope;
        }

        public static ParseContext FromRuntimeContext(Interpreter.RuntimeContext runtimeContext)
        {
            ParseContext parseContext = new ParseContext();

            // Iterate all variables from the runtime context's current scope and add them to the parse context's current scope
            using (var scopeEnumerator = new ScopeVariableEnumerator(runtimeContext.ScopeHandler.CurrentScope))
            {
                while (scopeEnumerator.MoveNext())
                {
                    var variableEntry = scopeEnumerator.Current;

                    parseContext.CurrentScope.VariableDeclarations.Add(new ParseContextScopeVariable(
                        variableEntry.Name,
                        new Word("", TType.Var),
                        variableEntry.DeclarationType,
                        false));
                }
            }

            return parseContext;
        }

        public bool CanScapeLoop()
        {
            return LoopStack.Empty || LoopStack.Peek();
        }

        public bool WithinFinally()
        {
            return !FinallyStack.Empty;
        }

        public bool WithinCatch()
        {
            return !CatchStack.Empty;
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
