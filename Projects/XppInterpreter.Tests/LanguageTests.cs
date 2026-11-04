using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XppInterpreter.Interpreter;
using XppInterpreter.Tests.Infrastructure;
using static XppInterpreter.Tests.Infrastructure.ScriptRunner;

namespace XppInterpreter.Tests
{
    [TestClass]
    public class OperatorTests
    {
        // In X++ && and || have the same precedence, against what its documentation says: pA01 and pD02 of the conformance file
        [TestMethod]
        public void AndOr_EvaluateFromLeftToRight() =>
            Assert.AreEqual("false", Run("boolean t = true; boolean f = false; info(any2Str(t || f && f));"));

        [TestMethod]
        public void AndOr_EvaluateFromLeftToRight_LeftSide() =>
            Assert.AreEqual("true", Run("boolean t = true; boolean f = false; info(any2Str(f && f || t));"));

        [TestMethod]
        public void UnaryMinus_AppliesOnlyToNextOperand() =>
            Assert.AreEqual("1", Run("int a = -1 + 2; info(any2Str(a));"));

        [TestMethod]
        public void UnaryMinus_OnVariable_AppliesOnlyToVariable() =>
            Assert.AreEqual("5", Run("int b = 5; int c = -b + 10; info(any2Str(c));"));

        [TestMethod]
        public void UnaryMinus_InComparison() =>
            Assert.AreEqual("true", Run("int x = -1; boolean t = true; info(any2Str(x == -1 && t));"));

        [TestMethod]
        public void Div_IsIntegerDivision() =>
            Assert.AreEqual("3", Run("info(any2Str(7 div 2));"));

        [TestMethod]
        public void Slash_IsRealDivision() =>
            Assert.AreEqual("3.50", Run("info(any2Str(7 / 2));"));

        [TestMethod]
        public void Mod_ReturnsRemainder() =>
            Assert.AreEqual("1", Run("info(any2Str(7 mod 3));"));

        [TestMethod]
        public void BinaryNot_IsSupported() =>
            Assert.AreEqual("-6", Run("info(any2Str(~5));"));

        [TestMethod]
        public void MultiplicationBeforeAddition() =>
            Assert.AreEqual("7", Run("info(any2Str(1 + 2 * 3));"));

        [TestMethod]
        public void Parentheses_OverridePrecedence() =>
            Assert.AreEqual("9", Run("info(any2Str((1 + 2) * 3));"));

        [TestMethod]
        public void HexLiteral_IsSupported() =>
            Assert.AreEqual("255", Run("info(any2Str(0xFF));"));

        [TestMethod]
        public void Int64Suffix_IsSupported() =>
            Assert.AreEqual("10", Run("int64 v = 10u; info(any2Str(v));"));

        [TestMethod]
        public void Comparison_AssignsToBoolean() =>
            Assert.AreEqual("true", Run("str s = 'x'; boolean b = s == 'x'; info(any2Str(b));"));

        [TestMethod]
        public void Logical_OnStrings_AssignsToBoolean() =>
            Assert.AreEqual("true", Run("str s = 'x'; boolean b = s && 'y'; info(any2Str(b));"));

        [TestMethod]
        public void Ternary_Works() =>
            Assert.AreEqual("yes", Run("int a = 2; str s = a > 1 ? 'yes' : 'no'; info(s);"));

        [TestMethod]
        public void Ternary_Nested() =>
            Assert.AreEqual("two", Run("int a = 2; info(a == 1 ? 'one' : a == 2 ? 'two' : 'many');"));
    }

    [TestClass]
    public class StatementTests
    {
        [TestMethod]
        public void If_WithoutBraces() =>
            Assert.AreEqual("a", Run("if (true) info('a');"));

        [TestMethod]
        public void IfElse_WithoutBraces() =>
            Assert.AreEqual("b", Run("if (false) info('a'); else info('b');"));

        [TestMethod]
        public void While_WithoutBraces() =>
            Assert.AreEqual("3", Run("int i; while (i < 3) i++; info(any2Str(i));"));

        [TestMethod]
        public void For_WithoutBraces() =>
            Assert.AreEqual("3", Run("int i; int n; for (i = 0; i < 3; i++) n++; info(any2Str(n));"));

        [TestMethod]
        public void Return_WithoutValue_InVoidFunction() =>
            Assert.AreEqual("before", Run("void f() { info('before'); return; info('after'); } f();"));

        [TestMethod]
        public void Return_WithValue() =>
            Assert.AreEqual("5", Run("int f() { return 5; } info(any2Str(f()));"));

        [TestMethod]
        public void DoWhile_Break_LeavesLoop() =>
            Assert.AreEqual("1", Run("int i; do { i++; break; } while (i < 5); info(any2Str(i));"));

        [TestMethod]
        public void DoWhile_Continue_EvaluatesCondition() =>
            Assert.AreEqual("3", Run("int i; do { i++; continue; } while (i < 3); info(any2Str(i));"));

        [TestMethod]
        public void While_BreakAndContinue() =>
            Assert.AreEqual("1,3", Run("int i; str r; while (i < 5) { i++; if (i == 2) { continue; } if (i == 4) { break; } r += r ? ',' + int2Str(i) : int2Str(i); } info(r);"));

        [TestMethod]
        public void For_BreakAndContinue() =>
            Assert.AreEqual("0,2", Run("int i; str r; for (i = 0; i < 5; i++) { if (i == 1) { continue; } if (i == 3) { break; } r += r ? ',' + int2Str(i) : int2Str(i); } info(r);"));

        [TestMethod]
        public void Switch_MatchesCase() =>
            Assert.AreEqual("two", Run("int a = 2; switch (a) { case 1: info('one'); break; case 2: info('two'); break; default: info('other'); }"));

        [TestMethod]
        public void Switch_NoDefault() =>
            Assert.AreEqual("two", Run("int a = 2; switch (a) { case 1: info('one'); break; case 2: info('two'); break; }"));

        [TestMethod]
        public void Switch_Default() =>
            Assert.AreEqual("other", Run("int a = 9; switch (a) { case 1: info('one'); break; default: info('other'); }"));

        [TestMethod]
        public void Switch_MultipleValuesInCase() =>
            Assert.AreEqual("low", Run("int a = 2; switch (a) { case 1, 2: info('low'); break; default: info('high'); }"));

        [TestMethod]
        public void Switch_FallsThroughWithoutBreak() =>
            Assert.AreEqual("a|b", Run("int x = 1; switch (x) { case 1: info('a'); case 2: info('b'); break; default: info('c'); }"));

        [TestMethod]
        public void BooleanAndGuid_CanBeDeclared() =>
            Assert.AreEqual("false", Run("boolean b; guid g; info(any2Str(b));"));

        [TestMethod]
        public void BooleanFunction_CanBeDeclared() =>
            Assert.AreEqual("true", Run("boolean f() { return true; } info(any2Str(f()));"));

        [TestMethod]
        public void StrWithLength_CanBeDeclared() =>
            Assert.AreEqual("abc", Run("str 10 s = 'abc'; info(s);"));

        [TestMethod]
        public void PrefixIncrement_IsSupported() =>
            Assert.AreEqual("1", Run("int i; ++i; info(any2Str(i));"));

        [TestMethod]
        public void PostfixDecrement_IsSupported() =>
            Assert.AreEqual("-1", Run("int i; i--; info(any2Str(i));"));

        [TestMethod]
        public void DefaultParameter_IsUsedWhenOmitted() =>
            Assert.AreEqual("5|9", Run("int f(int a = 5) { return a; } info(any2Str(f())); info(any2Str(f(9)));"));

        [TestMethod]
        public void Assignment_ConvertsToDeclaredType()
        {
            // real to int, the way a declared X++ variable keeps its type
            Assert.AreEqual("2", Run("int i; i = 5 div 2; i = i + 0.0; info(any2Str(i));"));
        }

        [TestMethod]
        public void StaticField_CanBeAssigned()
        {
            TestXppClass.StaticValue = 0;
            Run("TestXppClass::StaticValue = 11;");
            Assert.AreEqual(11, TestXppClass.StaticValue);
        }

        [TestMethod]
        public void EmptyStatement_IsValid_AsAStatement() =>
            Assert.AreEqual("1", Run("int a;\n;\na = 1;; info(any2Str(a));"));

        [TestMethod]
        public void EmptyStatement_IsNotValid_AsABody() =>
            StringAssert.Contains(string.Join(";", ParseErrors("int a; if (a == 0) ;")), "empty statement");

        [TestMethod]
        public void TopLevelReturn_StopsTheScript() =>
            Assert.AreEqual("a", Run("info('a'); return; info('b');"));

        [TestMethod]
        public void Like_WorksOutsideQueries() =>
            Assert.AreEqual("true", Run("str s = 'abc'; info(any2Str(s like 'A?C'));"));

        [TestMethod]
        public void NewStatement_CallsMethod() =>
            Assert.AreEqual("ok", Run("new TestXppClass().noReturn(); info('ok');"));

        [TestMethod]
        public void ArrayIndex_AcceptsInt64AndEnum() =>
            Assert.AreEqual("7 9", Run("int a[3]; int64 idx = 2; a[idx] = 7; a[TestXppEnum::Second] = 9; info(strFmt('%1 %2', a[2], a[1]));"));

        [TestMethod]
        public void DefaultParameter_CanUseEarlierParameters() =>
            Assert.AreEqual("3", Run("int f(int a, int b = a + 1) { return a + b; } info(any2Str(f(1)));"));

        [TestMethod]
        public void StringLength_TruncatesOnEveryAssignment() =>
            Assert.AreEqual("abc xyz", Run("str 3 s = 'abcdef'; str a = s; s = 'xyzabc'; info(strFmt('%1 %2', a, s));"));

        [TestMethod]
        public void RealToInt_Truncates() =>
            Assert.AreEqual("2 3 -2 3", Run("int i; real r = 2.5; i = r; int j = 3.7; int k; k = -2.5; int m; m = 3.5; info(strFmt('%1 %2 %3 %4', i, j, k, m));"));

        [TestMethod]
        public void Int64ToInt_WrapsAround() =>
            Assert.AreEqual("705032704", Run("int64 big = 5000000000; int i = big; info(any2Str(i));"));

        [TestMethod]
        public void LocalFunction_UsesAndChangesScriptVariables() =>
            Assert.AreEqual("2 2", Run("int n = 1; int f() { n++; return n; } int r = f(); info(strFmt('%1 %2', r, n));"));

        [TestMethod]
        public void LocalFunction_LocalsDoNotLeakToOtherFunctions() =>
            Assert.AreEqual("1", Run("int n = 1; int f() { return n; } int g() { int n = 5; return f(); } info(any2Str(g()));"));

        [TestMethod]
        public void Comments_AreIgnored() =>
            Assert.AreEqual("1", Run("// line\r\n/* block\r\n */ info('1');"));
    }

    [TestClass]
    public class EvaluationOrderTests
    {
        private const string Counter = "int n; int nx() { n++; return n; } ";

        [TestMethod]
        public void FunctionArguments_AreEvaluatedLeftToRight() =>
            Assert.AreEqual("1 2", Run(Counter + "info(strFmt('%1 %2', nx(), nx()));"));

        [TestMethod]
        public void DeclaredFunctionArguments_AreEvaluatedLeftToRight() =>
            Assert.AreEqual("12", Run(Counter + "str j(int a, int b) { return int2Str(a) + int2Str(b); } info(j(nx(), nx()));"));

        [TestMethod]
        public void ContainerElements_AreEvaluatedLeftToRight() =>
            Assert.AreEqual("1", Run(Counter + "container c = [nx(), nx()]; info(any2Str(conPeek(c, 1)));"));


        [TestMethod]
        public void BinaryOperands_AreEvaluatedLeftToRight() =>
            Assert.AreEqual("-1", Run(Counter + "info(any2Str(nx() - nx()));"));

        [TestMethod]
        public void CallerBeforeArguments()
        {
            DEVXppProbe.counter = 0;
            Assert.AreEqual("arg=2 counter=2", Run("info(DEVXppProbe::make().show(DEVXppProbe::nx()));"));
        }
    }

    [TestClass]
    public class ExpressionChainTests
    {
        [TestMethod]
        public void Constructor_MemberCall_InsideBinaryRightOperand() =>
            Assert.AreEqual("42", Run("info(any2Str(1 + new TestXppClass().getNumber()));"));

        [TestMethod]
        public void Constructor_MemberCall_InsideBinaryLeftOperand() =>
            Assert.AreEqual("42", Run("info(any2Str(new TestXppClass().getNumber() + 1));"));

        [TestMethod]
        public void Parenthesized_MemberCall_InsideBinary() =>
            Assert.AreEqual("42", Run("info(any2Str(1 + (new TestXppClass()).getNumber()));"));

        [TestMethod]
        public void MemberCall_Chain() =>
            Assert.AreEqual("43", Run("TestXppClass c = new TestXppClass(); info(any2Str(c.next().next().getNumber()));"));

        [TestMethod]
        public void StaticCall_InExpression() =>
            Assert.AreEqual("8", Run("info(any2Str(1 + TestXppClass::staticMethod()));"));

        [TestMethod]
        public void ObjectVariable_TakesAnyMethod() =>
            Assert.AreEqual("41", Run("Object o = new TestXppClass(); int n = o.getNumber(); info(any2Str(n));"));

        [DataTestMethod]
        [DataRow("c.variableArguments('a')", "a0")]
        [DataRow("c.variableArguments('a', 1)", "a1")]
        [DataRow("c.variableArguments('a', 1, 'x', 2)", "a3")]
        public void KernelMethods_WithVariableArguments(string call, string expected) =>
            Assert.AreEqual(expected, Run($"TestXppClass c = new TestXppClass(); info({call});"));

        [TestMethod]
        public void OtherClassVariable_ChecksTheMethod() =>
            Assert.IsTrue(System.Linq.Enumerable.Any(ParseErrors("TestXppClass c = new TestXppClass(); c.noSuchMethod();")));

        [TestMethod]
        public void Enum_Member() =>
            Assert.AreEqual("true", Run("TestXppEnum e = TestXppEnum::Second; info(any2Str(e == TestXppEnum::Second));"));
    }

    [TestClass]
    public class LoopGuardTests
    {
        private static XppInterpreterOptions Guard(int iterations) => new XppInterpreterOptions { MaxLoopIterations = iterations };

        [TestMethod]
        public void While_WithFalseIf_DoesNotBreakTheGuard() =>
            Assert.AreEqual("3", Run("int i; while (i < 3) { if (i == 9) { info('no'); } i++; } info(any2Str(i));", Guard(100)));

        [TestMethod]
        public void For_WithFalseIf_DoesNotBreakTheGuard() =>
            Assert.AreEqual("3", Run("int i; int n; for (i = 0; i < 3; i++) { if (i == 9) { info('no'); } n++; } info(any2Str(n));", Guard(100)));

        [TestMethod]
        public void While_WithIgnoredFunctionResult_DoesNotBreakTheGuard() =>
            Assert.AreEqual("3", Run("int f() { return 1; } int i; while (i < 3) { f(); i++; } info(any2Str(i));", Guard(100)));

        [TestMethod]
        public void While_AfterCaughtException_DoesNotBreakTheGuard() =>
            Assert.AreEqual("3", Run(
                "int two(int a, int b) { return a + b; } int i; " +
                "while (i < 3) { try { two(1, TestXppClass::boomInt()); } catch { i++; } } info(any2Str(i));", Guard(100)));

        [TestMethod]
        public void While_StopsAtGuard() =>
            StringAssert.Contains(
                Assert.ThrowsException<Exception>(() => Run("while (true) { }", Guard(10))).Message,
                "Max loop");

        [TestMethod]
        public void For_StopsAtGuard() =>
            StringAssert.Contains(
                Assert.ThrowsException<Exception>(() => Run("int i; for (i = 0; i < 1; i = 0) { }", Guard(10))).Message,
                "Max loop");

        [TestMethod]
        public void DoWhile_StopsAtGuard() =>
            StringAssert.Contains(
                Assert.ThrowsException<Exception>(() => Run("do { } while (true);", Guard(10))).Message,
                "Max loop");

        [TestMethod]
        public void Guard_AllowsExactLimit() =>
            Assert.AreEqual("10", Run("int i; while (i < 10) { i++; } info(any2Str(i));", Guard(10)));

        [TestMethod]
        public void NestedLoops_HaveTheirOwnGuard() =>
            Assert.AreEqual("9", Run("int a; int b; int n; for (a = 0; a < 3; a++) { for (b = 0; b < 3; b++) { n++; } } info(any2Str(n));", Guard(3)));
    }

    [TestClass]
    public class ExceptionTests
    {
        [TestMethod]
        public void Catch_All() =>
            Assert.AreEqual("caught", Run("try { throw Exception::Error; } catch { info('caught'); }"));

        [TestMethod]
        public void Catch_ByMember() =>
            Assert.AreEqual("err", Run("try { throw Exception::Error; } catch (Exception::Numeric) { info('num'); } catch (Exception::Error) { info('err'); }"));

        [TestMethod]
        public void Finally_Runs() =>
            Assert.AreEqual("try|fin", Run("try { info('try'); } finally { info('fin'); }"));

        [TestMethod]
        public void CatchAndFinally_Run() =>
            Assert.AreEqual("c|f", Run("try { throw Exception::Error; } catch { info('c'); } finally { info('f'); }"));

        [TestMethod]
        public void Retry_RunsTryAgain() =>
            Assert.AreEqual("2", Run("int n; try { n++; if (n < 2) { throw Exception::Error; } } catch { retry; } info(any2Str(n));"));

        [TestMethod]
        public void Uncaught_Propagates() =>
            Assert.ThrowsException<FakeXppException>(() => Run("throw Exception::Error;"));

        [TestMethod]
        public void Uncaught_KeepsOriginalStackTrace()
        {
            var ex = Assert.ThrowsException<FakeXppException>(() => Run("TestXppClass::boom();"));
            StringAssert.Contains(ex.StackTrace, nameof(TestXppClass.boom));
        }
    }

    [TestClass]
    public class FinallyExitTests
    {
        [TestMethod]
        public void Return_RunsFinally() =>
            Assert.AreEqual("try fin", Run("str r; str f() { try { return 'try'; } finally { r = 'fin'; } return 'end'; } str v = f(); info(strFmt('%1 %2', v, r));"));

        [TestMethod]
        public void Return_RunsFinally_OfEveryLevel() =>
            Assert.AreEqual("v ab", Run("str r; str f() { try { try { return 'v'; } finally { r += 'a'; } } finally { r += 'b'; } return 'end'; } str v = f(); info(strFmt('%1 %2', v, r));"));

        [TestMethod]
        public void Return_InLoop_RunsFinally() =>
            Assert.AreEqual("1 01", Run("str r; int f() { int i; for (i = 0; i < 3; i++) { try { if (i == 1) { return i; } } finally { r += int2Str(i); } } return -1; } int v = f(); info(strFmt('%1 %2', v, r));"));

        [TestMethod]
        public void Return_FromCatch_RunsFinally() =>
            Assert.AreEqual("c f", Run("str r; str f() { try { throw Exception::Error; } catch { return 'c'; } finally { r = 'f'; } return 'end'; } str v = f(); info(strFmt('%1 %2', v, r));"));

        [TestMethod]
        public void Return_WithoutValue_RunsFinally() =>
            Assert.AreEqual("f", Run("str r; void f() { try { return; } finally { r = 'f'; } } f(); info(r);"));

        [TestMethod]
        public void Break_RunsFinally() =>
            Assert.AreEqual("f 0", Run("str r; int i; for (i = 0; i < 3; i++) { try { break; } finally { r += 'f'; } } info(strFmt('%1 %2', r, i));"));

        [TestMethod]
        public void Continue_RunsFinally() =>
            Assert.AreEqual("ff 2", Run("str r; int i; for (i = 0; i < 2; i++) { try { continue; } finally { r += 'f'; } } info(strFmt('%1 %2', r, i));"));

        [TestMethod]
        public void Break_DoesNotLeaveAStaleHandler() =>
            Assert.AreEqual("outer", Run("int i; str r; try { for (i = 0; i < 2; i++) { try { break; } catch { r += 'inner'; } } throw Exception::Error; } catch { r += 'outer'; } info(r);"));

        [TestMethod]
        public void Continue_DoesNotLeaveAStaleHandler() =>
            Assert.AreEqual("outer", Run("int i; str r; try { for (i = 0; i < 2; i++) { try { continue; } catch { r += 'inner'; } } throw Exception::Error; } catch { r += 'outer'; } info(r);"));

        [TestMethod]
        public void Break_InWhileInsideTry_StaysInTheTry() =>
            Assert.AreEqual("1 caught", Run("int i; str r; try { while (true) { i++; break; } throw Exception::Error; } catch { r = 'caught'; } info(strFmt('%1 %2', i, r));"));

        [TestMethod]
        public void Break_InSwitchInsideTry() =>
            Assert.AreEqual("a f", Run("str r; int x = 1; try { switch (x) { case 1: r = 'a'; break; default: r = 'b'; } } finally { r += ' f'; } info(r);"));

        [TestMethod]
        public void UnhandledException_RunsFinally_ThenPropagates() =>
            Assert.AreEqual("fo", Run("str r; try { try { throw Exception::Error; } catch (Exception::Numeric) { r += 'n'; } finally { r += 'f'; } } catch { r += 'o'; } info(r);"));

        [TestMethod]
        public void Retry_LateInTheScript() =>
            Assert.AreEqual("2", Run("int a1; int a2; int a3; int a4; int a5; int a6; int a7; int a8; int a9; int a10; int a11; int a12; int n; try { n++; if (n < 2) { throw Exception::Error; } } catch { retry; } info(any2Str(n));"));
    }

    [TestClass]
    public class TransactionOptionTests
    {
        [TestMethod]
        public void GlobalAbort_Off_NeverAborts()
        {
            var host = Execute("info('x');", new XppInterpreterOptions { EmitGlobalTtsAbort = false });
            Assert.AreEqual(0, host.TtsBeginCount);
            Assert.AreEqual(0, host.TtsAbortCount);
        }

        [TestMethod]
        public void GlobalAbort_On_BeginsAndAborts()
        {
            var host = Execute("info('x');", new XppInterpreterOptions { EmitGlobalTtsAbort = true });
            Assert.AreEqual(1, host.TtsBeginCount);
            Assert.AreEqual(1, host.TtsAbortCount);
        }

        [TestMethod]
        public void NoOptions_NeverAborts()
        {
            var host = Execute("info('x');");
            Assert.AreEqual(0, host.TtsAbortCount);
        }
    }
}
