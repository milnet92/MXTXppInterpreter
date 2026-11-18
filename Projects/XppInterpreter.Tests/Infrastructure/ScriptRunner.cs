using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;
using XppInterpreter.Parser;

namespace XppInterpreter.Tests.Infrastructure
{
    /// <summary>
    /// Runs a script through the real pipeline: lexer, parser, bytecode generator, interpreter.
    /// </summary>
    public static class ScriptRunner
    {
        public static ParseResult Parse(string source, FakeXppHost host = null)
        {
            host = host ?? new FakeXppHost();
            return new XppParser(new XppLexer(source), host.Proxy).Parse();
        }

        public static IEnumerable<string> ParseErrors(string source, FakeXppHost host = null) =>
            Parse(source, host).Errors.Select(e => e.Message);

        public static FakeXppHost Execute(string source, XppInterpreterOptions options = null, FakeXppHost host = null)
        {
            host = host ?? new FakeXppHost();

            Interpret(source, options, host);

            return host;
        }

        /// <summary>Runs the script as MXTXppInterpreterExecutor::execute does, and returns the result of the interpreter.</summary>
        public static InterpreterResult Interpret(string source, XppInterpreterOptions options = null, FakeXppHost host = null)
        {
            host = host ?? new FakeXppHost();

            var result = Parse(source, host);

            Assert.IsTrue(
                result.IsCorrect,
                "The script should parse, but got: " + string.Join("; ", result.Errors.Select(e => e.Message)));

            return new Interpreter.XppInterpreter(host.Proxy, options).Interpret((Program)result.AST);
        }

        /// <summary>Runs the script and returns what its top level return statement gives.</summary>
        public static object Evaluate(string source, FakeXppHost host = null)
        {
            host = host ?? new FakeXppHost();

            var result = Parse(source, host);

            Assert.IsTrue(
                result.IsCorrect,
                "The script should parse, but got: " + string.Join("; ", result.Errors.Select(e => e.Message)));

            var interpreter = new Interpreter.XppInterpreter(host.Proxy);
            var byteCode = interpreter.Compile((Program)result.AST);
            var context = new RuntimeContext(host.Proxy, byteCode);

            interpreter.Interpret(byteCode, context);

            return context.ReturnValue;
        }

        /// <summary>Runs the script and returns the info messages, joined by |.</summary>
        public static string Run(string source, XppInterpreterOptions options = null) =>
            string.Join("|", Execute(source, options).Infos);
    }
}
