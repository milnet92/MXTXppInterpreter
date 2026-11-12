using System;
using System.Collections.Generic;
using System.Linq;
using XppInterpreter.Core;
using XppInterpreter.Interpreter.Bytecode;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Lexer;

namespace XppInterpreter.Interpreter
{
    /// <summary>
    /// A function of a compiled script that a host calls with its own values.
    /// A record or an object is the one of the caller, so what the function changes in it is changed for the caller.
    /// </summary>
    public sealed class ScriptFunction
    {
        private readonly RefFunction _function;
        private readonly ByteCode _byteCode;
        private readonly XppProxy _proxy;
        private readonly System.Type _returnType;

        public string Name => _function.Declaration.Name;
        public int ParameterCount => _function.Declaration.Parameters.Count;
        public int RequiredParameterCount { get; }
        /// <summary>
        /// The variables of the script that the function uses, itself or through the functions it calls, separated by commas.
        /// Empty for a function that can be called: those variables don't exist in a call.
        /// </summary>
        public string ScriptVariableNames { get; }

        internal ScriptFunction(RefFunction function, List<RefFunction> declaredFunctions, XppProxy proxy)
        {
            _function = function;
            _proxy = proxy;
            _byteCode = new ByteCode(function.Instructions) { DeclaredFunctions = declaredFunctions };

            RequiredParameterCount = function.Declaration.Parameters.Count(p => p.DefaultValue is null);
            ScriptVariableNames = string.Join(", ", FindScriptVariables(function));

            if (function.Declaration.Type.TokenType != TType.Void)
            {
                _returnType = function.Declaration.ReturnClrType
                    ?? proxy.Casting.GetSystemTypeFromTypeName(((Word)function.Declaration.Type).Lexeme);
            }
        }

        private static SortedSet<string> FindScriptVariables(RefFunction function)
        {
            var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<RefFunction>();
            var pending = new Stack<RefFunction>();

            pending.Push(function);

            while (pending.Count > 0)
            {
                RefFunction current = pending.Pop();

                if (!visited.Add(current))
                {
                    continue;
                }

                names.UnionWith(current.Declaration.ScriptVariables);

                foreach (DeclaredFunctionCall call in current.Instructions.OfType<DeclaredFunctionCall>())
                {
                    pending.Push(call.Function);
                }
            }

            return names;
        }

        public bool Accepts(int argumentCount)
        {
            return argumentCount >= RequiredParameterCount && argumentCount <= ParameterCount;
        }

        /// <summary>
        /// Runs the function in the transaction of the caller. An exception of the script leaves through here.
        /// </summary>
        /// <returns>The returned value, converted to the return type of the function. Null for a void function</returns>
        public object Call(object[] arguments)
        {
            arguments = arguments ?? new object[0];

            if (!Accepts(arguments.Length))
            {
                throw new ArgumentException($"The function '{Name}' can't be called with {arguments.Length} arguments.");
            }

            if (ScriptVariableNames.Length > 0)
            {
                throw new InvalidOperationException(
                    $"The function '{Name}' uses {ScriptVariableNames}, declared outside the functions of the script. In a call those variables don't exist.");
            }

            // The scope of the script is not there: the statements outside the functions didn't run
            RuntimeContext context = new RuntimeContext(_proxy, _byteCode);

            _function.BindArguments(context.ScopeHandler.CurrentScope, arguments, _proxy);

            new XppInterpreter(_proxy).Interpret(_byteCode, context);

            object value = context.ReturnValue;

            if (value is null || _returnType is null || _returnType == typeof(object) || value.GetType() == _returnType)
            {
                return value;
            }

            return Scope.ConvertValue(value, _returnType, _proxy);
        }
    }
}
