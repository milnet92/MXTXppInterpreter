using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XppInterpreter.Core;
using XppInterpreter.Tests.Infrastructure;
using static XppInterpreter.Tests.Infrastructure.ScriptRunner;

namespace XppInterpreter.Tests
{
    /// <summary>
    /// What the metadata says about a type or a member is asked once, not on every iteration of a loop: on an AOS each
    /// question is a lookup in the data dictionary, or a scan of the hundreds of members of a table.
    /// Scripts\RunBenchmark.ps1 measures what that costs.
    /// </summary>
    [TestClass]
    public class LookupCachingTests
    {
        private static int Lookups(string script, int iterations, Func<FakeXppHost, int> counter)
        {
            var host = new FakeXppHost();

            Execute(script.Replace("%N", iterations.ToString(CultureInfo.InvariantCulture)), host: host);

            return counter(host);
        }

        /// <summary>The parser asks too, so the number is compared between a short and a long loop.</summary>
        private static void AssertLookupsDoNotGrow(string script, Func<FakeXppHost, int> counter) =>
            Assert.AreEqual(Lookups(script, 2, counter), Lookups(script, 200, counter));

        [TestMethod]
        public void EnumLiteral_InLoop_IsResolvedOnce()
        {
            const string script = "TestXppEnum e = TestXppEnum::Second; int i, n; for (i = 0; i < %N; i++) { if (e == TestXppEnum::Second) { n++; } }";

            AssertLookupsDoNotGrow(script, host => host.EnumValueCalls);
            AssertLookupsDoNotGrow(script, host => host.IsEnumCalls);
        }

        [TestMethod]
        public void EnumLiteral_InLoop_KeepsItsValue() =>
            Assert.AreEqual("3 Second", Evaluate("int i, n; TestXppEnum e; for (i = 0; i < 3; i++) { e = TestXppEnum::Second; if (e == TestXppEnum::Second) { n++; } } return strFmt('%1 %2', n, e);"));

        [TestMethod]
        public void StaticField_InLoop_IsReadEveryTime() =>
            Assert.AreEqual(6, Evaluate("int i, total; for (i = 1; i <= 3; i++) { TestXppClass::StaticValue = i; total += TestXppClass::StaticValue; } return total;"));

        [TestMethod]
        public void StringAssignment_InLoop_AsksOnceIfTheTypeIsAnEdt() =>
            AssertLookupsDoNotGrow("str s; int i; for (i = 0; i < %N; i++) { s = 'a'; }", host => host.IsStringEdtCalls);

        [TestMethod]
        public void StringDeclaration_InLoop_AsksOnceIfTheTypeIsAnEdt() =>
            AssertLookupsDoNotGrow("int i; for (i = 0; i < %N; i++) { str s = 'a'; }", host => host.IsStringEdtCalls);

        [TestMethod]
        public void MethodCall_InLoop_AsksOnceIfTheTypeIsXpp() =>
            AssertLookupsDoNotGrow("TestXppClass c = new TestXppClass(); int i, n; for (i = 0; i < %N; i++) { n += c.getNumber(); }", host => host.IsXppTypeCalls);

        [TestMethod]
        public void FieldRead_InLoop_AsksOnceIfTheTypeIsXpp() =>
            AssertLookupsDoNotGrow("TestCommon t; str s; int i; t.AccountNum = 'a'; for (i = 0; i < %N; i++) { s = t.AccountNum; }", host => host.IsXppTypeCalls);

        [TestMethod]
        public void FieldRead_InLoop_ReadsTheCurrentValue() =>
            Assert.AreEqual("a0a1a2", Evaluate("TestCommon t; str s; int i; for (i = 0; i < 3; i++) { t.AccountNum = strFmt('a%1', i); s += t.AccountNum; } return s;"));

        [TestMethod]
        public void MemberOfXppObject_InLoop_ReadsTheCurrentValue() =>
            Assert.AreEqual(6, Evaluate("TestXppClass c = new TestXppClass(); int i, total; for (i = 1; i <= 3; i++) { c.Number = i; total += c.Number; } return total;"));

        [TestMethod]
        public void VariableArguments_InLoop_ArePackedForEachCall() =>
            Assert.AreEqual("a0 a1 a2 41", Evaluate(
                "TestXppClass c = new TestXppClass(); str s; int i;" +
                "for (i = 0; i < 3; i++) { if (i == 0) { s += c.variableArguments('a'); } else if (i == 1) { s += ' ' + c.variableArguments('a', 1); } else { s += ' ' + c.variableArguments('a', 1, 2); } }" +
                "return strFmt('%1 %2', s, c.getNumber());"));

        [TestMethod]
        public void PackVariableArguments_OnlyTheMethodThatEndsWithAnArray()
        {
            object[] arguments = { "a", 1, 2 };

            CollectionAssert.AreEqual(new object[] { 1, 2 }, (object[])ReflectionHelper.PackVariableArguments(typeof(TestXppClass), "VariableArguments", arguments)[1]);
            Assert.AreSame(arguments, ReflectionHelper.PackVariableArguments(typeof(TestXppClass), "getNumber", arguments));
            Assert.AreSame(arguments, ReflectionHelper.PackVariableArguments(typeof(TestXppClass), "noSuchMethod", arguments));
        }

        [TestMethod]
        public void IsDelegateField_OnlyForADelegate()
        {
            Assert.IsTrue(ReflectionHelper.IsDelegateField(typeof(WithDelegate), "CHANGED"));
            Assert.IsFalse(ReflectionHelper.IsDelegateField(typeof(WithDelegate), "Size"));
            Assert.IsFalse(ReflectionHelper.IsDelegateField(typeof(WithDelegate), "noSuchField"));
        }

        #region Overloads of .NET methods
        // The overload of a call is kept for the types of its arguments: another type, or a null of another declared type, chooses again

        [TestMethod]
        public void Overload_SameCallWithOtherValueTypes_IsChosenForEachType() =>
            Assert.AreEqual("int long decimal int", Evaluate(
                "int64 big = 5; real r = 1.5; container c = [1, big, r, 2]; anytype v; str s; int i;" +
                "for (i = 1; i <= conLen(c); i++) { v = conPeek(c, i); s += (i > 1 ? ' ' : '') + ClrFixtures.Overloads::Number(v); }" +
                "return s;"));

        [TestMethod]
        public void Overload_NullOfOtherDeclaredTypes_IsChosenForEachType() =>
            Assert.AreEqual("string builder object string", Evaluate(
                "System.String s; ClrFixtures.StringBuilderLike b; System.Object o;" +
                "return strFmt('%1 %2 %3 %4', ClrFixtures.Overloads::Nulls(s), ClrFixtures.Overloads::Nulls(b), ClrFixtures.Overloads::Nulls(o), ClrFixtures.Overloads::Nulls(s));"));

        [TestMethod]
        public void Overload_InLoop_GivesTheSameResultEveryTime() =>
            Assert.AreEqual("aaaa1212", Evaluate(
                "System.Text.StringBuilder sb = new System.Text.StringBuilder(); int i;" +
                "for (i = 0; i < 4; i++) { sb.Append('a'); }" +
                "for (i = 0; i < 2; i++) { sb.Append(1); sb.Append(2); }" +
                "return sb.ToString();"));
        #endregion

        #region Names without culture
        private static void WithTurkishCulture(Action action)
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;

            // In Turkish the lower case of "I" is not "i"
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            try
            {
                action();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [TestMethod]
        public void MemberNames_WithTurkishCulture_HaveNoCase() => WithTurkishCulture(() =>
        {
            Assert.IsTrue(ReflectionHelper.TypeHasProperty(typeof(System.IO.FileInfo), "isreadonly"));
            Assert.AreEqual("IsReadOnly", ReflectionHelper.GetProperty(typeof(System.IO.FileInfo), "ISREADONLY").Name);
            Assert.IsTrue(ReflectionHelper.TypeHasField(typeof(WithDelegate), "SIZE", false));
            Assert.AreEqual("Size", ReflectionHelper.GetField(typeof(WithDelegate), "size").Name);
            Assert.IsTrue(ReflectionHelper.TypeHasMethod(typeof(WithDelegate), "RAISE"));
        });

        [TestMethod]
        public void StaticMemberOfXppClass_WithTurkishCulture_IsFoundInAnyCase() => WithTurkishCulture(() =>
            Assert.AreEqual(3, Evaluate("TestXppClass::StaticValue = 3; return TestXppClass::STATICVALUE;")));

        [TestMethod]
        public void Variable_WithTurkishCulture_IsFoundInAnyCase() => WithTurkishCulture(() =>
            Assert.AreEqual(6, Evaluate("int ITEMID = 5; itemid++; return ItemId;")));

        [TestMethod]
        public void IntrinsicFunction_WithTurkishCulture_IsFoundInAnyCase() => WithTurkishCulture(() =>
            Assert.IsTrue(Interpreter.Proxy.XppProxyHelper.IsIntrinsicFunction("FIELDNUM")));
        #endregion

        /// <summary>A delegate of an X++ class is a public field of the compiled class.</summary>
        private sealed class WithDelegate
        {
            public Action Changed = () => { };
            public int Size = 1;

            public void Raise() => Changed?.Invoke();
        }
    }
}
