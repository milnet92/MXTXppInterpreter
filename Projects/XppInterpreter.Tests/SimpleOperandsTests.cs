using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XppInterpreter.Core;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Bytecode;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Interpreter.Query;
using XppInterpreter.Lexer;
using XppInterpreter.Parser;
using XppInterpreter.Tests.Infrastructure;
using static XppInterpreter.Tests.Infrastructure.ScriptRunner;

namespace XppInterpreter.Tests
{
    /// <summary>
    /// What the interpreter computes itself instead of asking the X++ proxies: operators on numbers, the values of a
    /// where clause, the breakpoint of a statement. The values are the ones of the compiler: conformance pN40 to pN50.
    /// </summary>
    [TestClass]
    public class SimpleOperandsTests
    {
        private static object Compute(TType operation, object left, object right)
        {
            Assert.IsTrue(SimpleOperands.TryEvaluate(operation, left, right, out object result), $"{operation} on {left} and {right} should need no proxy");

            return result;
        }

        private static bool NeedsTheProxies(TType operation, object left, object right) =>
            !SimpleOperands.TryEvaluate(operation, left, right, out _);

        [TestMethod]
        public void Integers_InALoop_NeverGoToTheProxies()
        {
            var host = Execute("int i, n; int64 big = 1; for (i = 0; i < 20; i++) { n += i * 2 - 1; big = big + i; if (n >= 5 && big != n) { n--; } }");

            Assert.AreEqual(0, host.NumberOperationCalls);
        }

        [TestMethod]
        public void Integers_WrapAround()
        {
            Assert.AreEqual(int.MinValue, Compute(TType.Plus, int.MaxValue, 1));
            Assert.AreEqual(int.MaxValue, Compute(TType.Minus, int.MinValue, 1));
            Assert.AreEqual(-2, Compute(TType.Star, int.MaxValue, 2));
            Assert.AreEqual(int.MinValue, Compute(TType.Star, int.MinValue, -1));
            Assert.AreEqual(long.MinValue, Compute(TType.Plus, long.MaxValue, 1L));
            Assert.AreEqual(-2L, Compute(TType.Star, long.MaxValue, 2L));
        }

        [TestMethod]
        public void IntegerWithInt64_GivesAnInt64()
        {
            Assert.AreEqual(2147483648L, Compute(TType.Plus, int.MaxValue, 1L));
            Assert.AreEqual(long.MinValue, Compute(TType.Increment, long.MaxValue, 1));
            Assert.AreEqual(6L, Compute(TType.Star, 2L, 3));
        }

        [TestMethod]
        public void Numbers_CompareByValue_WhateverTheirTypes()
        {
            Assert.AreEqual(true, Compute(TType.Equal, 5, 5L));
            Assert.AreEqual(true, Compute(TType.Equal, 5, 5.0m));
            Assert.AreEqual(true, Compute(TType.Smaller, 1, 2.5m));
            Assert.AreEqual(true, Compute(TType.GreaterOrEqual, 5000000000L, 1m));
            Assert.AreEqual(false, Compute(TType.Greater, 3, 5));
            Assert.AreEqual(true, Compute(TType.SmallerOrEqual, 3, 3));
            Assert.AreEqual(true, Compute(TType.NotEqual, 0.1m, 0.2m));
        }

        [TestMethod]
        public void BooleansAndValuesOfOneEnum_AreEqualByValue()
        {
            Assert.AreEqual(true, Compute(TType.Equal, true, true));
            Assert.AreEqual(true, Compute(TType.NotEqual, true, false));
            Assert.AreEqual(true, Compute(TType.Equal, TestXppEnum.Second, TestXppEnum.Second));
            Assert.AreEqual(false, Compute(TType.Equal, TestXppEnum.Second, TestXppEnum.Third));
        }

        [TestMethod]
        public void EverythingElse_GoesToTheProxies()
        {
            Assert.IsTrue(NeedsTheProxies(TType.Plus, 1.5m, 2.5m), "real + real");
            Assert.IsTrue(NeedsTheProxies(TType.Plus, 1, 2.5m), "int + real");
            Assert.IsTrue(NeedsTheProxies(TType.Division, 6, 3), "int / int is a real");
            Assert.IsTrue(NeedsTheProxies(TType.IntegerDivision, 6, 0), "div");
            Assert.IsTrue(NeedsTheProxies(TType.Mod, 6, 4), "mod");
            Assert.IsTrue(NeedsTheProxies(TType.LeftShift, 1, 32), "shift");
            Assert.IsTrue(NeedsTheProxies(TType.Equal, "a", "A"), "strings have no case");
            Assert.IsTrue(NeedsTheProxies(TType.Plus, "a", 1), "str + int");
            Assert.IsTrue(NeedsTheProxies(TType.Equal, TestXppEnum.Second, 1), "enum == int");
            Assert.IsTrue(NeedsTheProxies(TType.Greater, TestXppEnum.Second, TestXppEnum.First), "enum > enum");
            Assert.IsTrue(NeedsTheProxies(TType.Equal, true, 1), "boolean == int");
            Assert.IsTrue(NeedsTheProxies(TType.Equal, null, 1), "null");
        }

        [TestMethod]
        public void Condition_OnANumber_IsTrueWhenNotZero() =>
            Assert.AreEqual("yes no yes no yes", Evaluate(
                "int five = 5, zero; int64 big = 5000000000; real nothing; real half = 0.5;" +
                "return strFmt('%1 %2 %3 %4 %5', five ? 'yes' : 'no', zero ? 'yes' : 'no', big ? 'yes' : 'no', nothing ? 'yes' : 'no', half ? 'yes' : 'no');"));

        [TestMethod]
        public void Switch_OnIntegersAndEnums_MatchesByValue() =>
            Assert.AreEqual("two big second", Evaluate(
                "str name(int _value) { switch (_value) { case 1: return 'one'; case 2, 3: return 'two'; } return 'none'; }" +
                "str size(int64 _value) { switch (_value) { case 5000000000: return 'big'; } return 'small'; }" +
                "str which(TestXppEnum _value) { switch (_value) { case TestXppEnum::First: return 'first'; case TestXppEnum::Second: return 'second'; } return 'other'; }" +
                "return strFmt('%1 %2 %3', name(3), size(5000000000), which(TestXppEnum::Second));"));

        #region Values of a where clause
        private static QueryGenerationHelper HelperWith(string variable, object value, out FakeXppHost host)
        {
            host = new FakeXppHost();

            var context = new RuntimeContext(host.Proxy, new ByteCode(new List<IInstruction>()));

            context.ScopeHandler.CurrentScope.SetVar(variable, value, host.Proxy, true, value.GetType());

            return new QueryGenerationHelper(context);
        }

        [TestMethod]
        public void QueryValue_OfALiteral_IsTheLiteral()
        {
            var helper = HelperWith("x", 7, out _);

            Assert.AreEqual("DE-001", helper.ComputeExpression(new Constant("DE-001", null)));
            Assert.AreEqual(5, helper.ComputeConstant(new Constant(5, null)));
            Assert.AreEqual(true, helper.ComputeExpression(new Constant(true, null)));
            Assert.AreEqual(false, helper.ComputeExpression(new Constant(false, null)));
        }

        [TestMethod]
        public void QueryValue_OfAVariable_IsItsCurrentValue()
        {
            var helper = HelperWith("account", "US-001", out _);

            Assert.AreEqual("US-001", helper.ComputeVariable(new Variable(new Word("ACCOUNT", TType.Id), null, false, null)));
        }
        #endregion

        #region Debugger
        private const string Loop = "int i, n;\nfor (i = 0; i < 3; i++)\n{\n    n += i;\n}\ninfo(any2Str(n));";

        [TestMethod]
        public void Debugger_WithoutBreakpoints_RunsToTheEnd()
        {
            var host = new FakeXppHost();
            var program = (Program)Parse(Loop, host).AST;
            var interpreter = new Interpreter.XppInterpreter(host.Proxy) { Debugger = new XppDebugger(program) };

            Assert.IsTrue(interpreter.Interpret(program).HasFinished);
            Assert.AreEqual("3", host.Infos.Single());
        }

        [TestMethod]
        public void Debugger_WithABreakpoint_StopsThereOnEveryIteration()
        {
            var host = new FakeXppHost();
            var program = (Program)Parse(Loop, host).AST;
            var debugger = new XppDebugger(program);
            var interpreter = new Interpreter.XppInterpreter(host.Proxy) { Debugger = debugger };

            // Lines and columns start at 0: this is the statement inside the loop
            Assert.IsNotNull(debugger.TryAddBreakpoint(3, 4).NewBreakpoint);

            int stops = 0;
            var result = interpreter.Interpret(program);

            while (result.HasStopped)
            {
                stops++;
                Assert.AreEqual(3, result.Breakpoint.SourceCodeMapping.From.Line);

                result = interpreter.Continue(result, DebugAction.Continue);
            }

            Assert.AreEqual(3, stops);
            Assert.AreEqual("3", host.Infos.Single());
        }
        #endregion
    }
}
