using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XppInterpreter.Interpreter;
using XppInterpreter.Tests.Infrastructure;

namespace XppInterpreter.Tests
{
    /// <summary>
    /// A host calls the functions of a script by name, with its own values: MXTXppScript on an AOS.
    /// The record and object arguments of a compiled caller can only be tried on an AOS.
    /// </summary>
    [TestClass]
    public class CompiledScriptTests
    {
        private static ScriptFunction Function(string source, string name, FakeXppHost host = null)
        {
            var script = new CompiledScript(source, (host ?? new FakeXppHost()).Proxy);

            Assert.IsTrue(script.IsCorrect, "The script should parse, but got: " + string.Join("; ", script.ParseResult.Errors.Select(e => e.Message)));

            ScriptFunction function = script.FindFunction(name);

            Assert.IsNotNull(function, $"The script should have the function {name}");

            return function;
        }

        private static object Call(string source, string name, params object[] arguments) => Function(source, name).Call(arguments);

        [TestMethod]
        public void Call_PassesArgumentsAndReturnsTheValue() =>
            Assert.AreEqual(5, Call("int add(int a, int b) { return a + b; }", "add", 2, 3));

        [TestMethod]
        public void Call_FindsTheFunctionInAnyCase() =>
            Assert.AreEqual(5, Call("int add(int a, int b) { return a + b; }", "ADD", 2, 3));

        [TestMethod]
        public void Call_DoesNotRunTheStatementsOutsideTheFunctions()
        {
            var host = new FakeXppHost();

            Assert.AreEqual(1, Function("int one() { return 1; } info('test of the editor');", "one", host).Call(null));
            Assert.AreEqual(0, host.Infos.Count);
        }

        [TestMethod]
        public void Call_ChangesTheRecordOfTheCaller()
        {
            var record = new TestCommon();

            Call("void change(TestCommon _record) { _record.AccountNum = 'changed'; }", "change", record);

            Assert.AreEqual("changed", record.GetField("AccountNum"));
        }

        [TestMethod]
        public void Call_ChangesTheObjectOfTheCaller()
        {
            var instance = new TestXppClass();

            Call("void change(TestXppClass _instance) { _instance.Number = 7; }", "change", instance);

            Assert.AreEqual(7, instance.Number);
        }

        [TestMethod]
        public void Call_DoesNotChangeAValueOfTheCaller()
        {
            object[] arguments = { 1 };

            Assert.AreEqual(6, Function("int change(int a) { a += 5; return a; }", "change").Call(arguments));
            Assert.AreEqual(1, arguments[0]);
        }

        [TestMethod]
        public void Call_UsesTheDefaultValueOfAParameterThatIsNotPassed()
        {
            ScriptFunction function = Function("int add(int a, int b = 10) { return a + b; }", "add");

            Assert.AreEqual(11, function.Call(new object[] { 1 }));
            Assert.AreEqual(3, function.Call(new object[] { 1, 2 }));
            Assert.AreEqual(1, function.RequiredParameterCount);
            Assert.AreEqual(2, function.ParameterCount);
        }

        [TestMethod]
        public void Call_RefusesAWrongNumberOfArguments()
        {
            ScriptFunction function = Function("int add(int a, int b = 10) { return a + b; }", "add");

            Assert.IsFalse(function.Accepts(0));
            Assert.IsFalse(function.Accepts(3));
            Assert.ThrowsException<ArgumentException>(() => function.Call(new object[0]));
            Assert.ThrowsException<ArgumentException>(() => function.Call(new object[] { 1, 2, 3 }));
        }

        [TestMethod]
        public void Call_ConvertsTheArgumentToTheTypeOfTheParameter() =>
            Assert.AreEqual(2.5m, Call("real half(real a) { return a / 2; }", "half", 5));

        [TestMethod]
        public void Call_ConvertsTheReturnValueToTheReturnType() =>
            Assert.AreEqual(5m, Call("real same(int a) { return a; }", "same", 5));

        [TestMethod]
        public void Call_ReturnsNullForAVoidFunction() =>
            Assert.IsNull(Call("void nothing() { int a = 1; }", "nothing"));

        [TestMethod]
        public void Call_ReachesTheOtherFunctionsOfTheScript() =>
            Assert.AreEqual(240, Call(
                "int fact(int n) { return n <= 1 ? 1 : n * fact(n - 1); } int twice(int n) { return fact(n) * 2; }",
                "twice", 5));

        [TestMethod]
        public void Call_DoesNotSeeTheVariablesOfTheScript()
        {
            ScriptFunction function = Function("int total = 5; int read() { return total; }", "read");

            var exception = Assert.ThrowsException<Exception>(() => function.Call(null));

            StringAssert.Contains(exception.Message, "total");
        }

        [TestMethod]
        public void Call_LeavesTheTransactionToTheCaller()
        {
            var host = new FakeXppHost();

            Function("void save() { ttsbegin; ttscommit; }", "save", host).Call(null);

            Assert.AreEqual(1, host.TtsBeginCount);
            Assert.AreEqual(1, host.TtsCommitCount);
            Assert.AreEqual(0, host.TtsAbortCount);
        }

        [TestMethod]
        public void Call_LetsTheExceptionOfTheScriptThrough()
        {
            ScriptFunction function = Function("void fail() { throw Exception::Error; }", "fail");

            try
            {
                function.Call(null);
            }
            catch (Exception)
            {
                return;
            }

            Assert.Fail("The exception of the script should reach the caller");
        }

        private const string TryInside = "str tryInside() { try { throw Exception::Error; } catch { return 'caught inside'; } return 'not thrown'; }";

        [TestMethod]
        public void Call_OutsideATransaction_CatchesInTheFunction() =>
            Assert.AreEqual("caught inside", Call(TryInside, "tryInside"));

        /// <summary>
        /// A compiled method that is called inside a transaction can't catch an error either: it goes to the code outside the transaction
        /// </summary>
        [TestMethod]
        public void Call_InsideATransactionOfTheCaller_SkipsTheCatchOfTheFunction()
        {
            var host = new FakeXppHost();
            ScriptFunction function = Function(TryInside, "tryInside", host);

            host.Proxy.Data.TtsBegin();

            try
            {
                function.Call(null);
            }
            catch (Exception)
            {
                Assert.AreEqual(1, host.TtsLevelValue, "The transaction is the one of the caller: the function leaves it as it is");
                return;
            }

            Assert.Fail("The catch of the function should not run inside a transaction of the caller");
        }

        [TestMethod]
        public void Call_InsideATransactionOfTheCaller_CatchesAnUpdateConflictInTheFunction()
        {
            var host = new FakeXppHost();
            ScriptFunction function = Function(
                "str tryConflict() { try { throw Exception::UpdateConflict; } catch (Exception::UpdateConflict) { return 'caught inside'; } return 'not thrown'; }",
                "tryConflict", host);

            host.Proxy.Data.TtsBegin();

            Assert.AreEqual("caught inside", function.Call(null));
            Assert.AreEqual(1, host.TtsLevelValue);
        }

        [TestMethod]
        public void Call_AfterAnException_RunsAgain()
        {
            var script = new CompiledScript("void fail() { throw Exception::Error; } int ok() { return 1; }", new FakeXppHost().Proxy);

            try
            {
                script.FindFunction("fail").Call(null);
            }
            catch (Exception)
            {
            }

            Assert.AreEqual(1, script.FindFunction("ok").Call(null));
        }

        [TestMethod]
        public void Call_HasNoLoopLimit() =>
            Assert.AreEqual(200000, Call("int loop() { int i, n; for (i = 0; i < 200000; i++) { n++; } return n; }", "loop"));

        [TestMethod]
        public void Call_RunsOnSeveralThreadsAtOnce()
        {
            ScriptFunction function = Function(
                "int inc(int a) { return a + 1; } int run(int a) { int i, r = a; for (i = 0; i < 20; i++) { r = inc(r); } return r; }",
                "run");

            Parallel.For(0, 2000, i => Assert.AreEqual(i + 20, function.Call(new object[] { i })));
        }

        [TestMethod]
        public void Script_WithAnError_HasNoFunctions()
        {
            var script = new CompiledScript("int broken( { return 1; }", new FakeXppHost().Proxy);

            Assert.IsFalse(script.IsCorrect);
            Assert.IsTrue(script.ParseResult.Errors.Count > 0);
            Assert.IsNull(script.FindFunction("broken"));
        }

        [TestMethod]
        public void Script_WithoutThatFunction_ReturnsNull()
        {
            var script = new CompiledScript("int one() { return 1; }", new FakeXppHost().Proxy);

            Assert.IsNull(script.FindFunction("two"));
            Assert.IsNull(script.FindFunction(null));
        }

        [TestMethod]
        public void Cache_ParsesATextOnce()
        {
            var cache = new CompiledScriptCache(new FakeXppHost().Proxy);
            CompiledScript first = cache.Get("int one() { return 1; }");

            Assert.AreSame(first, cache.Get("int one() { return 1; }"));
            Assert.AreNotSame(first, cache.Get("int one() { return 2; }"));
            Assert.AreEqual(2, cache.Count);
        }

        [TestMethod]
        public void Cache_KeepsATextWithErrors()
        {
            var cache = new CompiledScriptCache(new FakeXppHost().Proxy);
            CompiledScript broken = cache.Get("int broken( {");

            Assert.IsFalse(broken.IsCorrect);
            Assert.AreSame(broken, cache.Get("int broken( {"));
            Assert.IsFalse(cache.Get(null).IsCorrect && cache.Get(null).FindFunction("any") != null);
        }

        [TestMethod]
        public void Cache_IsEmptiedWhenItIsFull()
        {
            var cache = new CompiledScriptCache(new FakeXppHost().Proxy, 2);

            cache.Get("int one() { return 1; }");
            cache.Get("int two() { return 2; }");
            CompiledScript third = cache.Get("int three() { return 3; }");

            Assert.AreEqual(1, cache.Count);
            Assert.AreSame(third, cache.Get("int three() { return 3; }"));
            Assert.AreEqual(1, cache.Get("int one() { return 1; }").FindFunction("one").Call(null));
        }
    }
}
