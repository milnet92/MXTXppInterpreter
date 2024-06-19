using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;
using DebugAction = XppInterpreter.Interpreter.Debug.DebugAction;

namespace XppInterpreter.Interpreter.Bytecode
{
    internal class DeclaredFunctionCall : IInstruction, ICall, IInterpretableInstruction
    {
        private RefFunction _ref;

        public string OperationCode => "CALL";
        public string Name => _ref.Declaration.Name;
        public int NArgs => _ref.Declaration.Parameters.Count;
        public bool Alloc => _ref.Declaration.ReturnType.TokenType != TType.Void;
        public bool ProcessParameters => true;
        public InterpreterResult LastResult { get; private set; }


        public DeclaredFunctionCall(RefFunction refFunction)
        {
            _ref = refFunction;
        }

        public void Execute(RuntimeContext context)
        {
            LastResult = Interpreter(context);
        }

        public InterpreterResult Interpreter(RuntimeContext context)
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
                    arguments[narg] = context.Stack.Pop();
                }

                // Create new runtime context for the function call
                ByteCode newByteCode = new ByteCode(_ref.Instructions)
                {
                    DeclaredFunctions = context.ByteCode.DeclaredFunctions
                };

                newContext = new RuntimeContext(context.Proxy, newByteCode);

                if (arguments.Length != NArgs)
                {
                    throw new Exception($"Definied function '{Name}' parameter count missmatch.");
                }

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

            InterpreterResult result = null;

            // Actual interpretation
            if (firstTime)
            { 
                result = interpreter.Interpret(newContext.ByteCode, newContext, nextAction: context.NextAction);
            }
            else
            {
                result = interpreter.Continue(newContext.ByteCode, newContext, context.NextAction);
            }
            if (newContext.Returned)
            {
                var ret = context.InnerContext.Stack.Pop();
                context.Stack.Push(ret);
            }

            return result;
        }
    }
}
