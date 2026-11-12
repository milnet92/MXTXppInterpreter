using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XppInterpreter.Core.Clr;
using XppInterpreter.Tests.Infrastructure;
using static XppInterpreter.Tests.Infrastructure.ScriptRunner;

namespace XppInterpreter.Tests
{
    /// <summary>
    /// What a finally block holds has to survive the statements inside it. The expected values come from the real compiler: see the pK cases of conformance.txt.
    /// </summary>
    [TestClass]
    public class FinallyBlockTests
    {
        private const string Thrower = "void thrower() { throw Exception::Error; } ";

        [TestMethod]
        public void NestedTry_KeepsTheReturnAndRunsTheRestOfTheBlock() =>
            Assert.AreEqual("1 11", Evaluate("int a; int f() { try { return 1; } finally { try { a = a + 1; } catch { } a = a + 10; } return 99; } int r = f(); return strFmt(\"%1 %2\", r, a);"));

        [TestMethod]
        public void NestedTryWithFinally_KeepsTheReturn() =>
            Assert.AreEqual("1 11", Evaluate("int a; int f() { try { return 1; } finally { try { a = 1; } finally { a += 10; } } return 99; } int r = f(); return strFmt(\"%1 %2\", r, a);"));

        [TestMethod]
        public void UsingStatement_KeepsTheReturn() =>
            Assert.AreEqual("1 11", Evaluate("int a; int f() { try { return 1; } finally { using (System.IO.StringWriter w = new System.IO.StringWriter()) { a = 1; } a += 10; } return 99; } int r = f(); return strFmt(\"%1 %2\", r, a);"));

        [TestMethod]
        public void ChangeCompany_KeepsTheReturn() =>
            Assert.AreEqual("1 11", Evaluate("int a; int f() { try { return 1; } finally { changecompany('usmf') { a = 1; } a += 10; } return 99; } int r = f(); return strFmt(\"%1 %2\", r, a);"));

        [TestMethod]
        public void ExceptionCaughtInTheBlock_KeepsTheReturn() =>
            Assert.AreEqual("1 5", Evaluate("int a; " + Thrower + "int f() { try { return 1; } finally { try { thrower(); } catch { a = 5; } } return 99; } int r = f(); return strFmt(\"%1 %2\", r, a);"));

        [TestMethod]
        public void BreakOfALoopInTheBlock_KeepsTheReturn() =>
            Assert.AreEqual("1 101", Evaluate("int a; int i; int f() { try { return 1; } finally { while (i < 3) { i++; try { break; } finally { a++; } } a += 100; } return 99; } int r = f(); return strFmt(\"%1 %2\", r, a);"));

        [TestMethod]
        public void NestedTry_KeepsABreak() =>
            Assert.AreEqual("2 11", Evaluate("int a; int i; while (i < 5) { i++; try { if (i == 2) { break; } } finally { try { a = a + 0; } catch { } if (i == 2) { a += 11; } } } return strFmt(\"%1 %2\", i, a);"));

        [TestMethod]
        public void ExceptionCaughtInTheBlock_KeepsTheExceptionOnItsWayOut() =>
            Assert.AreEqual("abd", Evaluate("str r; " + Thrower + "try { try { throw Exception::Error; } finally { try { thrower(); } catch { r += 'a'; } r += 'b'; } r += 'c'; } catch { r += 'd'; } return r;"));

        [TestMethod]
        public void NestedFinally_DoesNotReleaseTheExceptionEarly() =>
            Assert.AreEqual("abce", Evaluate("str r; try { try { throw Exception::Error; } finally { try { r += 'a'; } finally { r += 'b'; } r += 'c'; } r += 'd'; } catch { r += 'e'; } return r;"));

        [TestMethod]
        public void ExceptionLeavingTheBlock_ReplacesTheReturn() =>
            Assert.AreEqual("9 5", Evaluate("int a; " + Thrower + "int f() { try { try { return 1; } finally { thrower(); } } catch { a = 5; } return 9; } int r = f(); return strFmt(\"%1 %2\", r, a);"));

        [TestMethod]
        public void BlockLeftByAnException_DoesNotAffectTheNextOne() =>
            Assert.AreEqual("abcd", Evaluate("str r; " + Thrower + "try { try { r += 'a'; } finally { thrower(); } } catch { r += 'b'; } try { r += 'c'; } finally { r += 'd'; } return r;"));

        // The compiler crashes on this one, so it has no expected value of its own: pK31
        [TestMethod]
        public void RetryThroughAFinallyInTheCatchBlock() =>
            Assert.AreEqual("fx", Evaluate("int n; str r; try { n++; if (n == 1) { throw Exception::Error; } r += 'x'; } catch { try { retry; } finally { r += 'f'; } } return r;"));

        [TestMethod]
        public void ThrowInFinally_IsRefused() =>
            Assert.IsFalse(Parse("try { } finally { throw Exception::Error; }").IsCorrect);

        [TestMethod]
        public void ThrowInATryInsideFinally_IsRefused() =>
            Assert.IsFalse(Parse("try { } finally { try { throw Exception::Error; } catch { } }").IsCorrect);
    }

    [TestClass]
    public class ReturnStatementTests
    {
        [TestMethod]
        public void WithoutValueInAFunctionWithAType_IsRefused() =>
            Assert.IsFalse(Parse("int f() { return; } f();").IsCorrect);

        [TestMethod]
        public void WithValueInAVoidFunction_IsRefused() =>
            Assert.IsFalse(Parse("void f() { return 1; } f();").IsCorrect);

        [TestMethod]
        public void WithoutValueInAVoidFunction_IsValid() =>
            Assert.IsTrue(Parse("void f() { return; } f();").IsCorrect);

        [TestMethod]
        public void TheScriptCanReturnEitherWay()
        {
            Assert.IsTrue(Parse("return;").IsCorrect);
            Assert.IsTrue(Parse("return 1;").IsCorrect);
        }

        [TestMethod]
        public void TheFunctionDecidesAfterAnInnerOne() =>
            Assert.IsFalse(Parse("int f() { void g() { return; } g(); return; } f();").IsCorrect);
    }

    [TestClass]
    public class RetryCountTests
    {
        [TestMethod]
        public void LocalFunctionWithATry_DoesNotChangeTheCountOfTheCaller()
        {
            var host = new FakeXppHost();

            Evaluate("int i; void f() { try { i = i + 0; } catch { } } try { if (i == 0) { i = 1; throw Exception::Error; } f(); i = 5; } catch { retry; } return int2Str(i);", host);

            Assert.AreEqual(1, host.RetryCounts.Last());
        }

        [TestMethod]
        public void LocalFunctionThatThrows_DoesNotChangeTheCountOfTheCaller()
        {
            var host = new FakeXppHost();

            Evaluate("int n; void f() { try { throw Exception::Error; } catch (Exception::Numeric) { } } try { n++; if (n == 1) { f(); } n = n + 0; } catch { retry; } return int2Str(n);", host);

            Assert.AreEqual(1, host.RetryCounts.Last());
        }

        [TestMethod]
        public void NothingChanged_TheProxyIsNotCalledAgain()
        {
            var host = new FakeXppHost();

            Evaluate("int i; try { i = 1; i = 2; i = 3; } catch { } return int2Str(i);", host);

            Assert.AreEqual(1, host.RetryCounts.Count);
        }
    }

    [TestClass]
    public class ReferencedAssemblyTests
    {
        // The second assembly is not named after the namespace: only the references lead to it
        [TestMethod]
        public void FindingATypeDoesNotSkipTheOtherReferences()
        {
            var host = new FakeXppHost
            {
                ReferencedAssemblies = new[]
                {
                    "System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                    "System.Data.Entity, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
                }
            };

            Assert.IsNotNull(host.Proxy.ClrTypes.Find("System.Web.Script.Serialization.JavaScriptSerializer"));
            Assert.IsNotNull(host.Proxy.ClrTypes.Find("System.Data.Objects.ObjectContext"));
        }
    }

    [TestClass]
    public class OverloadInferenceTests
    {
        [TestMethod]
        public void VarFromOverloads_TakesTheTypeOfTheArgument() =>
            Assert.AreEqual("1", Evaluate("var n = System.Math::Abs(-1); return any2Str(n);"));

        [TestMethod]
        public void VarFromOverloads_WithAReal() =>
            Assert.AreEqual("true", Evaluate("var n = System.Math::Abs(-1.5); return any2Str(n == 1.5);"));

        [TestMethod]
        public void ChainedOnOverloads() =>
            Assert.AreEqual("1", Evaluate("return System.Math::Abs(-1).ToString();"));

        [TestMethod]
        public void ArgumentsThatAreVariables() =>
            Assert.AreEqual("7", Evaluate("int a = 3; int b = 7; var n = System.Math::Max(a, b); return any2Str(n);"));

        [TestMethod]
        public void InferredTypeIsTheOverloadOfTheArgument()
        {
            Assert.AreEqual(typeof(int), ClrBinder.GetMethodReturnType(typeof(Math), "Abs", true, new[] { typeof(int) }));
            Assert.AreEqual(typeof(decimal), ClrBinder.GetMethodReturnType(typeof(Math), "Abs", true, new[] { typeof(decimal) }));
        }

        [TestMethod]
        public void UnknownArgumentType_SaysNothing() =>
            Assert.IsNull(ClrBinder.GetMethodReturnType(typeof(Math), "Abs", true, new Type[] { null }));

        [TestMethod]
        public void NoOverloadAcceptsTheArgument_SaysNothing() =>
            Assert.IsNull(ClrBinder.GetMethodReturnType(typeof(Math), "Abs", true, new[] { typeof(Guid) }));
    }

    [TestClass]
    public class DeclarationStatementTests
    {
        [TestMethod]
        public void UsesTheOneBefore() =>
            Assert.AreEqual("2", Evaluate("int a = 1, b = a + 1; return int2Str(b);"));

        [TestMethod]
        public void ConstUsesTheOneBefore() =>
            Assert.AreEqual("2", Evaluate("const int A = 1, B = A + 1; return int2Str(B);"));

        [TestMethod]
        public void ConstCannotUseAVariable() =>
            Assert.IsFalse(Parse("int a = 1; const int B = a + 1; return int2Str(B);").IsCorrect);

        [TestMethod]
        public void CannotUseTheOneAfter() =>
            Assert.IsFalse(Parse("int a = b, b = 1; return int2Str(a);").IsCorrect);

        [TestMethod]
        public void ArraysAreDeclaredToo() =>
            Assert.AreEqual("5", Evaluate("int a[2], b = 5; a[1] = b; return int2Str(a[1]);"));
    }

    [TestClass]
    public class FloatingPointTests
    {
        private const string Big = "System.Double a = System.Math::Pow(10.0, 30.0); System.Double b = 1.0; ";

        [TestMethod]
        public void ComparisonOfValuesARealCannotHold() =>
            Assert.AreEqual("true", Evaluate(Big + "return any2Str(a > b);"));

        [TestMethod]
        public void ArithmeticOfValuesARealCannotHold() =>
            Assert.AreEqual("true", Evaluate(Big + "System.Double two = 2.0; System.Double c = a * two; return any2Str(c > a);"));

        // Compiled X++ ends the AOS process on this one, so it has no expected value of its own: pK13
        [TestMethod]
        public void Negation() =>
            Assert.AreEqual("true", Evaluate(Big + "System.Double c = -a; return any2Str(c < a);"));

        [TestMethod]
        public void NotANumber() =>
            Assert.AreEqual("false true", Evaluate("System.Double a = System.Math::Sqrt(-1.0); return strFmt(\"%1 %2\", a == a, a != a);"));

        [TestMethod]
        public void FloatsStayFloats()
        {
            object result = ClrValues.TryEvaluate(Lexer.TType.Plus, 1.5f, 2.5f, new FakeXppHost().Proxy, out object sum) ? sum : null;

            Assert.AreEqual(4f, result);
        }
    }

    [TestClass]
    public class GenericTypeTests
    {
        [TestMethod]
        public void ViolatedConstraint_IsAParseError()
        {
            var result = Parse("System.Nullable<str> x; return '1';");

            Assert.IsFalse(result.IsCorrect);
            StringAssert.Contains(result.Errors.First().Message, "constraint");
        }
    }

    [TestClass]
    public class InspectorTests
    {
        [TestMethod]
        public void UnfinishedTask_ResultIsNotRead()
        {
            var source = new System.Threading.Tasks.TaskCompletionSource<int>();

            Assert.IsFalse(ClrValues.CanInspect(source.Task, "Result"));
            Assert.IsTrue(ClrValues.CanInspect(source.Task, "Status"));

            source.SetResult(1);

            Assert.IsTrue(ClrValues.CanInspect(source.Task, "Result"));
        }

        [TestMethod]
        public void LazyValue_OnlyWhenCreated()
        {
            int created = 0;
            var lazy = new Lazy<int>(() => ++created);

            Assert.IsFalse(ClrValues.CanInspect(lazy, "Value"));
            Assert.AreEqual(0, created);

            Assert.AreEqual(1, lazy.Value);
            Assert.IsTrue(ClrValues.CanInspect(lazy, "Value"));
        }

        [TestMethod]
        public void OtherProperties() =>
            Assert.IsTrue(ClrValues.CanInspect(new System.Text.StringBuilder(), "Length"));
    }
}
