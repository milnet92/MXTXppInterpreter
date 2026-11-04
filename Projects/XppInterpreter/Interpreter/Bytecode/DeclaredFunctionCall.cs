using System.Linq;
using System;
using XppInterpreter.Lexer;

namespace XppInterpreter.Interpreter.Bytecode
{
    internal class DeclaredFunctionCall : IInstruction, ICall, IInterpretableInstruction
    {
        private RefFunction _ref;
        private readonly bool _allocate;
        public string OperationCode => "CALL";
        public string Name => _ref.Declaration.Name;
        public int NArgs { get; set; }
        public bool Alloc => _allocate && _ref.Declaration.Type.TokenType != TType.Void;
        public bool ProcessParameters => true;
        public InterpreterResult LastResult { get; private set; }

        public DeclaredFunctionCall(RefFunction refFunction, int nArgs, bool allocate = true)
        {
            _ref = refFunction;
            _allocate = allocate;
            NArgs = nArgs;

            var parameters = _ref.Declaration.Parameters;

            if (nArgs > parameters.Count || parameters.Skip(nArgs).Any(p => p.DefaultValue is null))
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
                for (int narg = NArgs - 1; narg >= 0; narg--)
                {
                    if (context.Stack.Count > 0)
                        arguments[narg] = context.Stack.Pop();
                }

                // Create new runtime context for the function call
                ByteCode newByteCode = new ByteCode(_ref.Instructions)
                {
                    DeclaredFunctions = context.ByteCode.DeclaredFunctions
                };

                // The function sees the variables of the script
                newContext = new RuntimeContext(context.Proxy, newByteCode, new Core.ScopeHandler(context.Proxy, context.ScopeHandler.ClosureScope));

                // Assign callee parameter to function definition parameter's scope
                for (int numParam = 0; numParam < NArgs; numParam++)
                {
                    var funcParameter = _ref.Declaration.Parameters[numParam];
                    System.Type declarationType = _ref.Declaration.Parameters[numParam].DeclarationClrType;

                    System.Diagnostics.Debug.Assert(declarationType != null); // We MUST have a declaration type

                    if (arguments[numParam] is null && context.Proxy.Reflection.IsCommonType(declarationType))
                    {
                        object defaultValue = context.Proxy.Casting.GetDefaultValueForType(funcParameter.DeclarationType.Lexeme);
                        newContext.ScopeHandler.CurrentScope.SetVar(funcParameter.Name, defaultValue, context.Proxy.Casting, true, declarationType);
                    }
                    else
                    {
                        newContext.ScopeHandler.CurrentScope.SetVar(funcParameter.Name, arguments[numParam], context.Proxy.Casting, true, declarationType);
                    }
                }

                interpreter = new XppInterpreter(context.Proxy, context.Interpreter.Options)
                {
                    Debugger = context.Interpreter.Debugger,
                    RunsLocalFunction = true
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
            if (newContext.Returned && Alloc)
            {
                // Push the returned value into current context stack
                context.Stack.Push(newContext.ReturnValue);
            }

            return result;
        }
    }
}
