using System;
using System.Collections.Generic;
using XppInterpreter.Interpreter.Bytecode;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Lexer;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter
{
    /// <summary>
    /// A script that is parsed and compiled once, for a host that calls its functions by name.
    /// The statements outside the functions never run here: they are the test that the editor runs.
    /// Several threads can call at the same time: every call has its own context, and the instructions are only read.
    /// </summary>
    public sealed class CompiledScript
    {
        private readonly Dictionary<string, ScriptFunction> _functions = new Dictionary<string, ScriptFunction>(StringComparer.OrdinalIgnoreCase);

        public ParseResult ParseResult { get; }
        public bool IsCorrect => ParseResult.IsCorrect;

        public CompiledScript(string source, XppProxy proxy)
        {
            try
            {
                ParseResult = new XppParser(new XppLexer(source ?? string.Empty), proxy).Parse();

                if (ParseResult.IsCorrect)
                {
                    // No debugger and no options: a call has no breakpoints, no loop limit and no transaction of its own
                    ByteCode byteCode = new XppInterpreter(proxy).Compile((Program)ParseResult.AST);

                    foreach (RefFunction function in byteCode.DeclaredFunctions)
                    {
                        _functions[function.Declaration.Name] = new ScriptFunction(function, byteCode.DeclaredFunctions, proxy);
                    }
                }
            }
            catch (Exception exception)
            {
                // Kept as an error of the script, so that a text that can't be compiled is not compiled again on every call
                ParseResult = ParseResult ?? new ParseResult();
                ParseResult.Errors.Add(new ParseError(null, 0, 0, exception.Message));
                _functions.Clear();
            }
        }

        /// <returns>Null when the script has no function with that name</returns>
        public ScriptFunction FindFunction(string name)
        {
            return name != null && _functions.TryGetValue(name, out ScriptFunction function) ? function : null;
        }
    }
}
