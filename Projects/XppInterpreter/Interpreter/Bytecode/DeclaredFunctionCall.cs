using System;
using XppInterpreter.Lexer;

namespace XppInterpreter.Interpreter.Bytecode
{
    internal class DeclaredFunctionCall : IInstruction, ICall, IInterpretableInstruction
    {
        private RefFunction _ref;

        public string OperationCode => "CALL";
        public string Name => _ref.Declaration.Name;

        public int NArgs { get; set; }
        public bool Alloc => _ref.Declaration.Type.TokenType != TType.Void;
        public bool ProcessParameters => true;
        public InterpreterResult LastResult { get; private set; }


        public DeclaredFunctionCall(RefFunction refFunction, int nArgs)
        {
            _ref = refFunction;
            NArgs = nArgs;

            if (_ref.Declaration.Parameters.Count != nArgs)
            {
                throw new Exception($"Definied function '{Name}' parameter count missmatch.");
            }
        }

        public void Execute(RuntimeContext context)
        {
            LastResult = Interpret(context);
        }

        public InterpreterResult Interpret(RuntimeContext context)
        {
            RuntimeContext newContext = context.InnerContext;
            XppInterpreter interpreter = newContext?.Interpreter;
            bool firstTime = interpreter is null;

            if (newContext is null)
            {
                // Get parameters from stack
                object[] arguments = new object[NArgs];

                for (int narg = 0; narg < NArgs; narg++)
                {
                    if (context.Stack.Count > 0)
                        arguments[narg] = context.Stack.Pop();
                }

                // Create new runtime context for the function call
                ByteCode newByteCode = new ByteCode(_ref.Instructions)
                {
                    DeclaredFunctions = context.ByteCode.DeclaredFunctions
                };

                newContext = new RuntimeContext(context.Proxy, newByteCode);

                // Assign callee parameter to function definition parameter's scope
                for (int numParam = 0; numParam < NArgs; numParam++)
                {
                    var funcParameter = _ref.Declaration.Parameters[numParam];
                    string typeName = (funcParameter.Type as Word).Lexeme;
                    object defaultValue = context.Proxy.Casting.GetDefaultValueForType(typeName);
                    System.Type declarationType = context.Proxy.Casting.GetSystemTypeFromTypeName(typeName);

                    if (arguments[numParam] is null && context.Proxy.Reflection.IsCommonType(declarationType))
                    {
                        newContext.ScopeHandler.CurrentScope.SetVar(funcParameter.Name, defaultValue, true, true, declarationType);
                    }
                    else
                    {
                        newContext.ScopeHandler.CurrentScope.SetVar(funcParameter.Name, arguments[numParam], true, true, declarationType);
                    }
                }

                interpreter = new XppInterpreter(context.Proxy, context.Interpreter.Options)
                {
                    Debugger = context.Interpreter.Debugger
                };

                newContext.Interpreter = interpreter;
                context.InnerContext = newContext;

            }

            InterpreterResult result;

            // Actual interpretation
            if (firstTime)
            {
                result = interpreter.Interpret(newContext.ByteCode, newContext, nextAction: context.NextAction);
            }
            else
            {
                result = interpreter.Continue(newContext.ByteCode, newContext, context.NextAction);
            }
            
            // Check if interpreter modified return flag
            if (newContext.Returned)
            {
                // Push the inner context last value into current context stack
                context.Stack.Push(context.InnerContext.Stack.Pop());
            }

            return result;
        }
    }
}
