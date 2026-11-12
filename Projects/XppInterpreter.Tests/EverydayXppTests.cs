using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XppInterpreter.Core;
using XppInterpreter.Tests.Infrastructure;
using static XppInterpreter.Tests.Infrastructure.ScriptRunner;

namespace XppInterpreter.Tests
{
    // The behaviour itself comes from the conformance file: what is here are the refusals and what only a fake can show.
    [TestClass]
    public class MacroTests
    {
        private static FakeXppHost HostWithLibrary() => new FakeXppHost
        {
            MacroLibraries = name => name == "Lib" ? "// Comment\n#define.FromLib(7)\n#define.Other(8)" : null
        };

        [TestMethod]
        public void Library_LoadedByItsName() =>
            Assert.AreEqual(7, Evaluate("#Lib\nint a = #FromLib;\nreturn a;", HostWithLibrary()));

        [TestMethod]
        public void Library_LoadedWithMacroLib() =>
            Assert.AreEqual(8, Evaluate("#macrolib.Lib\nint a = #Other;\nreturn a;", HostWithLibrary()));

        [TestMethod]
        public void Library_LoadedTwice() =>
            Assert.AreEqual(7, Evaluate("#Lib\n#Lib\nint a = #FromLib;\nreturn a;", HostWithLibrary()));

        [TestMethod]
        public void UnknownMacro_IsAnError() =>
            Assert.IsFalse(Parse("#NoSuch\nreturn 1;").IsCorrect);

        [TestMethod]
        public void UnknownLibrary_IsAnError() =>
            Assert.IsFalse(Parse("#macrolib.NoSuch\nreturn 1;").IsCorrect);

        [TestMethod]
        public void ErrorsKeepTheirLine()
        {
            var errors = Parse("#localmacro.A\n    1\n#endmacro\n#define.B(2)\nint a = #A;\nint b = ;").Errors;

            Assert.AreEqual(5, errors.First().Line);
        }

        [TestMethod]
        public void MultilineMacro_DoesNotMoveTheNextLines()
        {
            var errors = Parse("#localmacro.A\n    1 +\n    2\n#endmacro\nint a = #A;\nint b = ;").Errors;

            Assert.AreEqual(5, errors.First().Line);
        }

        [TestMethod]
        public void CommentInsideMacro_IsRemoved() =>
            Assert.AreEqual(3, Evaluate("#localmacro.A\n    1 // one\n    + 2 /* two */\n#endmacro\nint a = #A;\nreturn a;"));

        [TestMethod]
        public void MacroThatUsesItself_DoesNotHang() =>
            Assert.IsFalse(Parse("#define.A(#A)\nint a = #A;\nreturn a;").IsCorrect);

        [TestMethod, Timeout(30000)]
        public void MacroThatUsesItselfTwice_DoesNotGrowWithoutEnd() =>
            Assert.IsFalse(Parse("#define.A(#A #A)\nint a = #A;\nreturn a;").IsCorrect);

        [TestMethod]
        public void ArgumentsInSeveralLines_DoNotMoveTheNextLines()
        {
            var errors = Parse("#localmacro.Add\n    (%1 + %2)\n#endmacro\nint a = #Add(1,\n    2);\nint b = ;").Errors;

            Assert.AreEqual(5, errors.First().Line);
        }

        [TestMethod]
        public void HashInsideString_StaysAsItIs() =>
            Assert.AreEqual("#a # b", Evaluate("return '#a # b';"));

        [TestMethod]
        public void HashInVerbatimString_StaysAsItIs() =>
            Assert.AreEqual("#a", Evaluate("#define.a(1)\nreturn @\"#a\";"));

        [TestMethod]
        public void IfWithoutEndIf_SkipsTheRest() =>
            Assert.IsNull(Evaluate("int a = 1;\n#if.NoSuch\na = 2;\nreturn a;"));
    }

    [TestClass]
    public class ConstTests
    {
        private static string Error(string source) => string.Join("; ", ParseErrors(source));

        [TestMethod]
        public void Assignment_IsRejected() =>
            StringAssert.Contains(Error("const int A = 1; A = 2;"), "cannot be changed");

        [TestMethod]
        public void CompoundAssignment_IsRejected() =>
            StringAssert.Contains(Error("const int A = 1; A += 2;"), "cannot be changed");

        [TestMethod]
        public void Increment_IsRejected() =>
            StringAssert.Contains(Error("const int A = 1; A++;"), "cannot be changed");

        [TestMethod]
        public void PrefixIncrement_IsRejected() =>
            StringAssert.Contains(Error("const int A = 1; ++A;"), "cannot be changed");

        [TestMethod]
        public void ContainerAssignment_IsRejected() =>
            StringAssert.Contains(Error("const int A = 1; int b; [A, b] = [1, 2];"), "cannot be changed");

        [TestMethod]
        public void ByRef_IsRejected() =>
            StringAssert.Contains(Error("const int A = 1; ClrFixtures.Overloads::TryDouble(1, byref A);"), "cannot be changed");

        [TestMethod]
        public void WithoutValue_IsRejected() =>
            StringAssert.Contains(Error("const int A;"), "requires a value");

        [TestMethod]
        public void FromVariable_IsRejected() =>
            StringAssert.Contains(Error("int i = 5; const int A = i;"), "must be constant");

        [TestMethod]
        public void FromFunction_IsRejected() =>
            StringAssert.Contains(Error("const str A = strFmt('x');"), "must be constant");

        [TestMethod]
        public void FromAnotherConst_IsAccepted() =>
            Assert.AreEqual(3, Evaluate("const int A = 1; const int B = A + 2; return B;"));

        [TestMethod]
        public void Negative_IsAccepted() =>
            Assert.AreEqual(-1, Evaluate("const int A = -1; return A;"));
    }

    [TestClass]
    public class AssignmentOperatorTests
    {
        [TestMethod]
        public void Multiply_OnArrayItem() =>
            Assert.AreEqual(6, Evaluate("int a[2]; a[1] = 2; a[1] *= 3; return a[1];"));

        [TestMethod]
        public void Divide_KeepsTheDeclaredType() =>
            Assert.AreEqual(2, Evaluate("int i = 5; i /= 2; return i;"));

        [DataTestMethod]
        [DataRow("|=")]
        [DataRow("&=")]
        [DataRow("^=")]
        [DataRow("%=")]
        public void Others_DoNotExist(string assignment) =>
            Assert.IsFalse(Parse($"int i = 2; i {assignment} 1;").IsCorrect);

        [TestMethod]
        public void DivisionThatLooksLikeAComment_IsADivision() =>
            Assert.AreEqual(2m, Evaluate("real r = 4; real s = r / 2; return s;"));
    }

    [TestClass]
    public class LiteralTests
    {
        [TestMethod]
        public void Exponent() => Assert.AreEqual(150m, Evaluate("return 1.5e2;"));

        [TestMethod]
        public void NegativeExponent() => Assert.AreEqual(0.015m, Evaluate("return 1.5e-2;"));

        [TestMethod]
        public void ExplicitPositiveExponent() => Assert.AreEqual(150m, Evaluate("return 1.5E+2;"));

        [TestMethod]
        public void ExponentWithoutDigits_IsAnError() => Assert.IsFalse(Parse("real r = 1.5e;").IsCorrect);

        [TestMethod]
        public void ExponentOutOfRange_IsAnError() => Assert.IsFalse(Parse("real r = 1.5e60;").IsCorrect);

        [TestMethod]
        public void UtcDateTime()
        {
            var value = (FakeUtcDateTime)Evaluate("return 2020-01-02T03:04:05;");

            Assert.AreEqual(new DateTime(2020, 1, 2, 3, 4, 5), value.Value);
        }

        [TestMethod]
        public void UtcDateTime_WithWrongMonth_IsAnError() =>
            Assert.IsFalse(Parse("utcdatetime u = 2020-13-02T03:04:05;").IsCorrect);

        [TestMethod]
        public void NumbersWithDashes_AreSubtractions() =>
            Assert.AreEqual(2017, Evaluate("return 2020-01-02;"));

        [TestMethod]
        public void NumbersWithDashesBeforeAVariable_AreSubtractions() =>
            Assert.AreEqual(2016, Evaluate("int T03 = 1; return 2020-01-02-T03;"));
    }

    [TestClass]
    public class LabelTests
    {
        [TestMethod]
        public void LabelId_IsTheTextOfTheLabel() =>
            Assert.AreEqual("Time transactions", Evaluate("return \"@SYS1\";"));

        [TestMethod]
        public void UnknownLabel_StaysAsItIs() =>
            Assert.AreEqual("@NOSUCH1", Evaluate("return \"@NOSUCH1\";"));

        [DataTestMethod]
        [DataRow("@SYS1 x")]
        [DataRow(" @SYS1")]
        [DataRow("@")]
        [DataRow("@1")]
        public void OtherText_StaysAsItIs(string text) =>
            Assert.AreEqual(text, Evaluate($"return \"{text}\";"));
    }

    [TestClass]
    public class SystemVariableTests
    {
        [TestMethod]
        public void Appl() => Assert.AreEqual(0, Evaluate("return appl.ttsLevel();"));

        [TestMethod]
        public void Appl_AnyCase() => Assert.AreEqual(0, Evaluate("return APPL.ttsLevel();"));

        [TestMethod]
        public void InTransaction() => Assert.AreEqual(1, Evaluate("ttsbegin; int level = appl.ttsLevel(); ttsabort; return level;"));

        [DataTestMethod]
        [DataRow("appl")]
        [DataRow("infolog")]
        [DataRow("classFactory")]
        public void CannotBeDeclared(string name) =>
            StringAssert.Contains(string.Join("; ", ParseErrors($"int {name} = 5;")), "cannot be declared");

        [TestMethod]
        public void AsMemberName_IsNotTheSystemVariable() =>
            StringAssert.Contains(string.Join("; ", ParseErrors("TestCommon t; str s = t.appl;")), "appl");

        [TestMethod]
        public void OtherNames_AreStillNotDeclared() =>
            StringAssert.Contains(string.Join("; ", ParseErrors("int a = nothing;")), "not declared");
    }

    [TestClass]
    public class ChangeCompanyTests
    {
        private static void AssertClosed(FakeXppHost host, int opened = 1)
        {
            Assert.AreEqual(opened, host.CompanyHandlersOpened);
            Assert.AreEqual(0, host.CompanyHandlersOpen);
        }

        [TestMethod]
        public void Block()
        {
            var host = new FakeXppHost();

            Assert.AreEqual(1, Evaluate("int a; changecompany('x') { a = 1; } return a;", host));
            AssertClosed(host);
        }

        [TestMethod]
        public void Return()
        {
            var host = new FakeXppHost();

            Assert.AreEqual(1, Evaluate("changecompany('x') { return 1; }", host));
            AssertClosed(host);
        }

        [TestMethod]
        public void ReturnInLocalFunction()
        {
            var host = new FakeXppHost();

            Assert.AreEqual(1, Evaluate("int f() { changecompany('x') { return 1; } return 0; } return f();", host));
            AssertClosed(host);
        }

        [TestMethod]
        public void Break()
        {
            var host = new FakeXppHost();

            Assert.AreEqual(1, Evaluate("int i; while (true) { i++; changecompany('x') { break; } } return i;", host));
            AssertClosed(host);
        }

        [TestMethod]
        public void Continue()
        {
            var host = new FakeXppHost();

            Assert.AreEqual(2, Evaluate("int i; while (i < 2) { i++; changecompany('x') { continue; } } return i;", host));
            AssertClosed(host, 2);
        }

        [TestMethod]
        public void Exception()
        {
            var host = new FakeXppHost();

            Assert.AreEqual(1, Evaluate("try { changecompany('x') { throw Exception::Error; } } catch { return 1; } return 0;", host));
            AssertClosed(host);
        }

        [TestMethod]
        public void ExceptionNotCaught()
        {
            var host = new FakeXppHost();

            Assert.ThrowsException<FakeXppException>(() => Evaluate("changecompany('x') { throw Exception::Error; }", host));
            AssertClosed(host);
        }

        [TestMethod]
        public void Retry()
        {
            var host = new FakeXppHost();

            Assert.AreEqual(2, Evaluate("int tries; try { tries++; if (tries < 2) { throw Exception::Error; } } catch { changecompany('x') { retry; } } return tries;", host));
            AssertClosed(host);
        }

        [TestMethod]
        public void RetryThroughTwoBlocks()
        {
            var host = new FakeXppHost();

            Assert.AreEqual(2, Evaluate("int tries; try { tries++; if (tries < 2) { throw Exception::Error; } } catch { changecompany('x') { changecompany('y') { retry; } } } return tries;", host));
            AssertClosed(host, 2);
        }

        [TestMethod]
        public void TryInside_CatchesItsException()
        {
            var host = new FakeXppHost();

            Assert.AreEqual(1, Evaluate("int a; changecompany('x') { try { throw Exception::Error; } catch { a = 1; } } return a;", host));
            AssertClosed(host);
        }

        [TestMethod]
        public void Unchecked_ClosesOnReturn()
        {
            var host = new FakeXppHost();

            Assert.AreEqual(1, Evaluate("unchecked(1) { return 1; }", host));
            Assert.AreEqual(0, host.UncheckedHandlersOpen);
        }
    }

    [TestClass]
    public class HandledExceptionTests
    {
        [TestMethod]
        public void FinallyInsideCatch_DoesNotThrowItAgain() =>
            Assert.AreEqual("fbg", Evaluate("str r; try { try { throw Exception::Error; } finally { r += 'f'; } } catch { try { r += 'b'; } finally { r += 'g'; } } return r;"));

        [TestMethod]
        public void ChangeCompanyInsideCatch_DoesNotThrowItAgain() =>
            Assert.AreEqual(1, Evaluate("int a; try { changecompany('x') { throw Exception::Error; } } catch { changecompany('y') { a = 1; } } return a;"));

        [TestMethod]
        public void UsingInsideCatch_DoesNotThrowItAgain() =>
            Assert.AreEqual(1, Evaluate("int a; try { using (var w = new System.IO.StringWriter()) { throw Exception::Error; } } catch { using (var w = new System.IO.StringWriter()) { a = 1; } } return a;"));

        [TestMethod]
        public void NewExceptionInsideCatch_IsTheOneThatEscapes() =>
            Assert.AreEqual("warning", Evaluate("try { try { try { throw Exception::Error; } finally { } } catch { try { } finally { } throw Exception::Warning; } } catch (Exception::Warning) { return 'warning'; } return 'none';"));

        [TestMethod]
        public void ExceptionThroughTwoFinally_StillEscapes() =>
            Assert.AreEqual("ab", Evaluate("str r; try { try { try { throw Exception::Error; } finally { r += 'a'; } } finally { r += 'b'; } } catch { return r; } return 'none';"));
    }

    [TestClass]
    public class GlobalTransactionTests
    {
        private static readonly Interpreter.XppInterpreterOptions RollBack = new Interpreter.XppInterpreterOptions { EmitGlobalTtsAbort = true };

        [TestMethod]
        public void Catch_StillRuns() =>
            Assert.AreEqual("c", Run("try { throw Exception::Error; } catch { info('c'); }", RollBack));

        [TestMethod]
        public void Catch_KeepsTheGlobalTransaction()
        {
            var host = Execute("try { ttsbegin; ttsbegin; throw Exception::Error; } catch { info(any2Str(appl.ttsLevel())); }", RollBack);

            Assert.AreEqual("1", string.Join("|", host.Infos));
            Assert.AreEqual(1, host.TtsAbortCount);
        }

        [TestMethod]
        public void TryInsideATransactionOfTheScript_DoesNotCatch() =>
            Assert.AreEqual("outer", Run("try { ttsbegin; try { throw Exception::Error; } catch { info('inner'); } ttscommit; } catch { info('outer'); }", RollBack));

        [TestMethod]
        public void InLocalFunction_CatchStillRuns() =>
            Assert.AreEqual("c", Run("void f() { try { throw Exception::Error; } catch { info('c'); } } f();", RollBack));
    }

    [TestClass]
    public class RecordTests
    {
        [TestMethod]
        public void FieldById_ReadAndWrite() =>
            Assert.AreEqual("x", Evaluate("TestCommon t; t.(5) = 'x'; return t.(5);"));

        [TestMethod]
        public void FieldById_CompoundAssignment() =>
            Assert.AreEqual("xy", Evaluate("TestCommon t; t.(5) = 'x'; t.(5) += 'y'; return t.(5);"));

        [TestMethod]
        public void FieldById_WithVariable() =>
            Assert.AreEqual("x", Evaluate("TestCommon t; int id = 5; t.(id) = 'x'; str s = t.(id); return s;"));

        [TestMethod]
        public void FieldById_AsArgument() =>
            Assert.AreEqual("x", Evaluate("str f(str _a) { return _a; } TestCommon t; t.(5) = 'x'; return f(t.(5));"));

        [TestMethod]
        public void FieldById_InContainerAssignment() =>
            Assert.AreEqual("x 1", Evaluate("TestCommon t; int i; [t.(5), i] = ['x', 1]; return strFmt('%1 %2', t.(5), i);"));

        [TestMethod]
        public void Orig_IsTheSameTable() =>
            Assert.AreEqual("a", Evaluate("TestCommon t; t.AccountNum = 'a'; return t.orig().AccountNum;"));

        [TestMethod]
        public void Data_IsTheSameTable() =>
            Assert.AreEqual("a", Evaluate("TestCommon t; t.AccountNum = 'a'; return t.data().AccountNum;"));

        [TestMethod]
        public void MapCall() =>
            Assert.AreEqual("map", Evaluate("TestCommon t; return t.TestMap::mapName();"));

        [TestMethod]
        public void MapCall_WithArgument() =>
            Assert.AreEqual("map 5", Evaluate("TestCommon t; return t.TestMap::mapValue(5);"));

        [TestMethod]
        public void MapCall_WithUnknownMap_IsAnError() =>
            StringAssert.Contains(string.Join("; ", ParseErrors("TestCommon t; return t.NoSuchMap::mapName();")), "NoSuchMap");
    }

    [TestClass]
    public class ArrayTests
    {
        [TestMethod]
        public void ArraysAndVariablesInOneStatement() =>
            Assert.AreEqual("1 2", Evaluate("int a[2], c, b[3]; c = 1; a[1] = c; b[3] = 2; return strFmt('%1 %2', a[1], b[3]);"));

        [TestMethod]
        public void InitializedVariableAfterArray() =>
            Assert.AreEqual(5, Evaluate("int a[2], c = 5; return c;"));

        [TestMethod]
        public void Assignment_SharesTheArray() =>
            Assert.AreEqual(2, Evaluate("int a[2], b[2]; a[1] = 1; b = a; b[1] = 2; return a[1];"));

        [TestMethod]
        public void Parameter_IsTheArrayOfTheCaller() =>
            Assert.AreEqual(9, Evaluate("void change(int _a[]) { _a[1] = 9; } int a[2]; change(a); return a[1];"));

        [TestMethod]
        public void ClrType_IsRejected() =>
            Assert.IsFalse(Parse("System.String a[2];").IsCorrect);
    }

    [TestClass]
    public class ConstructorArgumentTests
    {
        private class VariableArguments
        {
            public VariableArguments() { }
            public VariableArguments(object[] values) { }
        }

        private class TwoParameters
        {
            public TwoParameters(object[] values) { }
            public TwoParameters(string a, string b) { }
        }

        [TestMethod]
        public void Values_GoInAnArray()
        {
            var result = ReflectionHelper.PackConstructorArguments(typeof(VariableArguments), new object[] { "a", 1 });

            Assert.AreEqual(1, result.Length);
            CollectionAssert.AreEqual(new object[] { "a", 1 }, (object[])result[0]);
        }

        [TestMethod]
        public void NoValues_UseTheEmptyConstructor() =>
            Assert.AreEqual(0, ReflectionHelper.PackConstructorArguments(typeof(VariableArguments), new object[0]).Length);

        [TestMethod]
        public void AnArray_StaysAsItIs()
        {
            var array = new object[] { "a" };

            Assert.AreSame(array, ReflectionHelper.PackConstructorArguments(typeof(VariableArguments), new object[] { array })[0]);
        }

        [TestMethod]
        public void AMatchingConstructor_Wins() =>
            Assert.AreEqual(2, ReflectionHelper.PackConstructorArguments(typeof(TwoParameters), new object[] { "a", "b" }).Length);

        [TestMethod]
        public void AClassWithoutVariableArguments_StaysAsItIs() =>
            Assert.AreEqual(1, ReflectionHelper.PackConstructorArguments(typeof(string[]), new object[] { 1 }).Length);
    }
}
