using System;
using System.Linq;
using ClrFixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XppInterpreter.Core.Clr;
using XppInterpreter.Interpreter;
using XppInterpreter.Tests.Infrastructure;
using static XppInterpreter.Tests.Infrastructure.ScriptRunner;

namespace XppInterpreter.Tests
{
    [TestClass]
    public class ClrOverloadTests
    {
        private static string Call(string call) => (string)Evaluate("return ClrFixtures.Overloads::" + call + ";");

        [DataTestMethod]
        [DataRow("Number(1)", "int")]
        [DataRow("Number(10u)", "long")]
        [DataRow("Number(1.5)", "decimal")]
        [DataRow("WidenInt(1)", "long")]
        [DataRow("OnlyDouble(2)", "double 2")]
        [DataRow("OnlyDouble(1.5)", "double 1.5")]
        [DataRow("OnlyLong(2)", "long 2")]
        [DataRow("OnlyInt(10u)", "int 10")]
        [DataRow("OnlyInt(2.9)", "int 2")]
        [DataRow("OnlyByte(5)", "byte 5")]
        [DataRow("OnlyBool(true)", "bool True")]
        [DataRow("OnlyInt(true)", "int 1")]
        [DataRow("OnlyChar('a')", "char a")]
        [DataRow("OnlyDayOfWeek(2)", "day Tuesday")]
        [DataRow("OnlyDayOfWeek(System.DayOfWeek::Friday)", "day Friday")]
        [DataRow("StringOrObject('x')", "string")]
        [DataRow("StringOrObject(1)", "object")]
        [DataRow("ClassOrInterface(new ClrFixtures.Stream2())", "class")]
        [DataRow("ToBase(new ClrFixtures.Derived())", "base")]
        [DataRow("Derived(new ClrFixtures.Derived())", "derived")]
        [DataRow("Derived(new ClrFixtures.Base())", "base")]
        public void TheBestOverloadIsCalled(string call, string expected) =>
            Assert.AreEqual(expected, Call(call));

        [TestMethod]
        public void XppEnum_PrefersObject_OverItsIntegerValue() =>
            Assert.AreEqual("object", Call("EnumOrObject(TestXppEnum::Second)"));

        [TestMethod]
        public void XppEnum_ConvertsToInt_WhenThereIsNothingElse() =>
            Assert.AreEqual("int", Call("EnumOrInt(TestXppEnum::Second)"));

        [TestMethod]
        public void MethodNames_AreNotCaseSensitive() =>
            Assert.AreEqual("int", Call("NUMBER(1)"));

        [TestMethod]
        public void TypeNames_AreCaseSensitive() =>
            Assert.IsTrue(ParseErrors("str s = clrfixtures.overloads::Number(1);").Any());

        [DataTestMethod]
        [DataRow("Optional(1)", "1 default 7")]
        [DataRow("Optional(1, 'x')", "1 x 7")]
        [DataRow("Optional(1, 'x', 2)", "1 x 2")]
        [DataRow("OptionalOrExact(1)", "exact")]
        [DataRow("OptionalOrExact(1, 2)", "optional")]
        [DataRow("OptionalStruct(1)", "1 False")]
        public void OptionalParameters_CanBeOmitted(string call, string expected) =>
            Assert.AreEqual(expected, Call(call));

        [DataTestMethod]
        [DataRow("Params()", "")]
        [DataRow("Params('a')", "a")]
        [DataRow("Params('a', 'b', 'c')", "a,b,c")]
        [DataRow("ParamsAfter(1)", "1:0")]
        [DataRow("ParamsAfter(1, 'a', 2)", "1:2")]
        [DataRow("ParamsOrExact('a', 'b')", "exact")]
        [DataRow("ParamsOrExact('a', 'b', 'c')", "params")]
        [DataRow("ParamsChars('a', 'b')", "ab")]
        public void ParamsParameters_AreExpanded(string call, string expected) =>
            Assert.AreEqual(expected, Call(call));

        [TestMethod]
        public void ParamsParameters_AcceptAnArray() =>
            Assert.AreEqual("a,b", Evaluate("System.String[] v = new System.String[2](); v.SetValue('a', 0); v.SetValue('b', 1); return ClrFixtures.Overloads::Params(v);"));

        [DataTestMethod]
        [DataRow("NullableLong(5)", "value 5")]
        [DataRow("NullableLong(10u)", "value 10")]
        [DataRow("NullableLong(null)", "null")]
        public void NullableParameters(string call, string expected) =>
            Assert.AreEqual(expected, Call(call));

        [TestMethod]
        public void NullInObjectVariable_UsesTheObjectOverload() =>
            Assert.AreEqual("object", Evaluate("System.Object o; return ClrFixtures.Overloads::Nulls(o);"));

        [TestMethod]
        public void NullInTypedVariable_UsesTheOverloadOfItsType() =>
            Assert.AreEqual("builder", Evaluate("ClrFixtures.StringBuilderLike b; return ClrFixtures.Overloads::Nulls(b);"));

        [TestMethod]
        public void NullVariable_CannotGoToAStructParameter()
        {
            var ex = Assert.ThrowsException<MissingMethodException>(() => Evaluate("System.Object o; return ClrFixtures.Overloads::OnlyInt(o);"));
            StringAssert.Contains(ex.Message, "OnlyInt");
        }

        [TestMethod]
        public void NoOverload_SaysWhatExists()
        {
            var ex = Assert.ThrowsException<MissingMethodException>(() => Call("Number('text')"));
            StringAssert.Contains(ex.Message, "Number");
            StringAssert.Contains(ex.Message, "String");
            StringAssert.Contains(ex.Message, "System.Int64 value");
        }

        [TestMethod]
        public void GenericMethods_AreLeftOut() =>
            Assert.AreEqual("not generic", Call("Generic('x')"));

        [TestMethod]
        public void ByRef_WritesTheVariableBack() =>
            Assert.AreEqual("true 10", Evaluate("int v; boolean ok = ClrFixtures.Overloads::TryDouble(5, byref v); return strFmt('%1 %2', ok, v);"));

        [TestMethod]
        public void ByRef_ReadsAndWritesTheVariable() =>
            Assert.AreEqual("2 a!", Evaluate("int a = 1; str b = 'a'; ClrFixtures.Overloads::Swap(byref a, byref b); return strFmt('%1 %2', a, b);"));

        [TestMethod]
        public void ByRef_ConvertsToTheTypeOfTheVariable() =>
            Assert.AreEqual("10.00", Evaluate("real v; ClrFixtures.Overloads::TryDouble(5, byref v); return strFmt('%1', v);"));

        [TestMethod]
        public void ByRef_OnSomethingElse_IsAnError() =>
            StringAssert.Contains(string.Join(";", ParseErrors("ClrFixtures.Members m = new ClrFixtures.Members(); int v; ClrFixtures.Overloads::TryDouble(5, byref m.Number);")), "byref");
    }

    [TestClass]
    public class ClrTypeSyntaxTests
    {
        [TestMethod]
        public void UsingDirective_AppliesToStaticCalls() =>
            Assert.AreEqual("int", Evaluate("using ClrFixtures; return Overloads::Number(1);"));

        [TestMethod]
        public void UsingDirective_AppliesToDeclarations() =>
            Assert.AreEqual("base", Evaluate("using ClrFixtures; Base b = new Base(); return b.Who();"));

        [TestMethod]
        public void UsingAlias_ForANamespace() =>
            Assert.AreEqual("int", Evaluate("using F = ClrFixtures; return F.Overloads::Number(1);"));

        [TestMethod]
        public void UsingAlias_ForAType() =>
            Assert.AreEqual("int", Evaluate("using O = ClrFixtures.Overloads; return O::Number(1);"));

        [TestMethod]
        public void XppType_ComesBeforeUsing() =>
            Assert.AreEqual("42", Evaluate("using ClrFixtures; TestXppClass c = new TestXppClass(); return int2Str(c.next().getNumber());"));

        [TestMethod]
        public void LocalVariable_ComesBeforeNamespace() =>
            Assert.AreEqual("41", Evaluate("TestXppClass System = new TestXppClass(); return int2Str(System.Number);"));

        [TestMethod]
        public void UnknownDottedType_IsAParseError() =>
            StringAssert.Contains(string.Join(";", ParseErrors("No.Such.Type t;")), "No.Such.Type");

        [TestMethod]
        public void UnknownMember_IsAParseError() =>
            Assert.IsTrue(ParseErrors("System.Text.StringBuilder sb = new System.Text.StringBuilder(); sb.NoSuchMethod();").Any());

        [TestMethod]
        public void UnknownStaticMember_IsAParseError() =>
            Assert.IsTrue(ParseErrors("str s = System.IO.Path::NoSuchMethod('a');").Any());

        [TestMethod]
        public void GenericType_Declaration() =>
            Assert.AreEqual("String", Evaluate("ClrFixtures.GenericHolder<str> h = new ClrFixtures.GenericHolder<str>(); return h.TypeName();"));

        [DataTestMethod]
        [DataRow("str", "String")]
        [DataRow("int", "Int32")]
        [DataRow("int64", "Int64")]
        [DataRow("real", "Decimal")]
        [DataRow("boolean", "Boolean")]
        [DataRow("System.Double", "Double")]
        public void GenericType_ArgumentsAreXppOrClrTypes(string argument, string expected) =>
            Assert.AreEqual(expected, Evaluate($"var h = new ClrFixtures.GenericHolder<{argument}>(); return h.TypeName();"));

        [TestMethod]
        public void GenericType_TwoArguments() =>
            Assert.AreEqual("String,Int32", Evaluate("var h = new ClrFixtures.TwoArguments<str, int>(); return h.Names();"));

        [TestMethod]
        public void GenericType_Nested_ClosesWithShiftToken() =>
            Assert.AreEqual("GenericHolder`1", Evaluate("var h = new ClrFixtures.GenericHolder<ClrFixtures.GenericHolder<int>>(); return h.TypeName();"));

        [TestMethod]
        public void GenericType_StaticCall() =>
            Assert.AreEqual("static String", Evaluate("return ClrFixtures.GenericHolder<str>::StaticName();"));

        [TestMethod]
        public void GenericType_WithWrongArgumentCount_IsAnError() =>
            Assert.IsTrue(ParseErrors("ClrFixtures.GenericHolder<str, int> h;").Any());

        [TestMethod]
        public void LessThan_IsStillAnOperator() =>
            Assert.AreEqual("true", Evaluate("int a = 1; int b = 2; return any2Str(a < b);"));

        [TestMethod]
        public void Array_OneDimension() =>
            Assert.AreEqual("3 x", Evaluate("System.String[] a = new System.String[3](); a.SetValue('x', 1); return strFmt('%1 %2', a.Length, a.GetValue(1));"));

        [TestMethod]
        public void Array_SizeFromExpression() =>
            Assert.AreEqual("4", Evaluate("int n = 2; System.Int32[] a = new System.Int32[n * 2](); return int2Str(a.Length);"));

        [TestMethod]
        public void Array_TwoDimensions() =>
            Assert.AreEqual("6", Evaluate("System.Int32[,] a = new System.Int32[2, 3](); return int2Str(a.Length);"));

        [TestMethod]
        public void Array_ParenthesesAreOptional() =>
            Assert.AreEqual("2", Evaluate("System.Object[] a = new System.Object[2]; return int2Str(a.Length);"));

        [TestMethod]
        public void XppArrayOfClrType_IsAnError() =>
            Assert.IsTrue(ParseErrors("System.String a[3];").Any());

        [TestMethod]
        public void ClrTypes_InFunctionSignature() =>
            Assert.AreEqual("5", Evaluate("System.Text.StringBuilder f(System.Text.StringBuilder sb) { sb.Append('abcde'); return sb; } return int2Str(f(new System.Text.StringBuilder()).Length);"));

        [TestMethod]
        public void IsAndAs() =>
            Assert.AreEqual("true false text true", Evaluate(
                "System.Object o = 'text'; System.String s = o as System.String; System.Text.StringBuilder sb = o as System.Text.StringBuilder; " +
                "return strFmt('%1 %2 %3 %4', o is System.String, o is System.Text.StringBuilder, s, sb == null);"));

        [TestMethod]
        public void Is_OnNull_IsFalse() =>
            Assert.AreEqual("false", Evaluate("System.Object o; return any2Str(o is System.String);"));

        [TestMethod]
        public void VerbatimString_HasNoEscapes() =>
            Assert.AreEqual(@"c:\new\table", Evaluate(@"return @""c:\new\table"";"));

        [TestMethod]
        public void VerbatimString_CanHaveManyLines() =>
            Assert.AreEqual("a\nb", Evaluate("return @\"a\nb\";"));

        [TestMethod]
        public void VerbatimString_DoubledDelimiter() =>
            Assert.AreEqual("say \"hi\"", Evaluate("return @\"say \"\"hi\"\"\";"));

        [TestMethod]
        public void KeywordsCanBeInTypeNames_KeepingTheirCase() =>
            Assert.AreEqual("Int64", Evaluate("System.Int64 v = 1; System.Object o = v; return o.GetType().Name;"));
    }

    [TestClass]
    public class ClrMemberTests
    {
        [TestMethod]
        public void StaticField_ReadAndWrite()
        {
            Statics.Number = 3;
            Assert.AreEqual("8", Evaluate("ClrFixtures.Statics::Number = 8; return int2Str(ClrFixtures.Statics::Number);"));
            Statics.Number = 3;
        }

        [TestMethod]
        public void StaticProperty_ReadAndWrite()
        {
            Statics.Text = "text";
            Assert.AreEqual("new", Evaluate("ClrFixtures.Statics::Text = 'new'; return ClrFixtures.Statics::Text;"));
            Statics.Text = "text";
        }

        [TestMethod]
        public void StaticProperty_ConvertsTheValue()
        {
            Evaluate("ClrFixtures.Statics::Long = 5;");
            Assert.AreEqual(5L, Statics.Long);
        }

        [TestMethod]
        public void Constant() =>
            Assert.AreEqual("9", Evaluate("return int2Str(ClrFixtures.Statics::Constant);"));

        [TestMethod]
        public void NestedType_StaticCall() =>
            Assert.AreEqual("nested", Evaluate("return ClrFixtures.Statics.Nested::Name();"));

        [TestMethod]
        public void NestedEnum() =>
            Assert.AreEqual("Two", Evaluate("ClrFixtures.Statics.Kind k = ClrFixtures.Statics.Kind::Two; return k.ToString();"));

        [TestMethod]
        public void InstanceField_ReadAndWrite() =>
            Assert.AreEqual("5", Evaluate("ClrFixtures.Members m = new ClrFixtures.Members(); m.Number = 5; return int2Str(m.Number);"));

        [TestMethod]
        public void InstanceProperty_ConvertsTheValue() =>
            Assert.AreEqual("5", Evaluate("ClrFixtures.Members m = new ClrFixtures.Members(); m.LongProperty = 5; return int642Str(m.LongProperty);"));

        [TestMethod]
        public void InstanceField_ConvertsRealToDouble() =>
            Assert.AreEqual("1.5", Evaluate("ClrFixtures.Members m = new ClrFixtures.Members(); m.DoubleField = 1.5; return strFmt('%1', m.DoubleField);"));

        [TestMethod]
        public void MemberNames_AreNotCaseSensitive() =>
            Assert.AreEqual("5 method", Evaluate("ClrFixtures.Members m = new ClrFixtures.Members(); m.NUMBER = 5; return strFmt('%1 %2', m.number, m.casediffers());"));

        [TestMethod]
        public void Property_ThroughGetAndSetMethods() =>
            Assert.AreEqual("7", Evaluate("ClrFixtures.Members m = new ClrFixtures.Members(); m.set_LongProperty(7); return int642Str(m.get_LongProperty());"));

        [TestMethod]
        public void Indexer_ThroughItsMethod() =>
            Assert.AreEqual("indexed 3", Evaluate("ClrFixtures.Members m = new ClrFixtures.Members(); return m.get_Item(3);"));

        [TestMethod]
        public void IntToEnumProperty() =>
            Assert.AreEqual("Tuesday", Evaluate("ClrFixtures.Members m = new ClrFixtures.Members(); m.Day = 2; return m.Day.ToString();"));

        [TestMethod]
        public void NullableProperty_WithValue() =>
            Assert.AreEqual("5", Evaluate("ClrFixtures.Item i = new ClrFixtures.Item('a'); int64 size = i.FileSize; return int642Str(size);"));

        [TestMethod]
        public void NullableProperty_WithoutValue_IsTheDefaultOfTheVariable() =>
            Assert.AreEqual("0", Evaluate("ClrFixtures.Item i = new ClrFixtures.Item('a'); int64 size = i.NoSize; return int642Str(size);"));

        [TestMethod]
        public void MethodOnNull_SaysWhatHappened()
        {
            var ex = Assert.ThrowsException<NullReferenceException>(() => Evaluate("System.Text.StringBuilder sb; sb.Append('x');"));
            StringAssert.Contains(ex.Message, "Append");
        }

        [TestMethod]
        public void PropertyOnNull_SaysWhatHappened()
        {
            var ex = Assert.ThrowsException<NullReferenceException>(() => Evaluate("System.Text.StringBuilder sb; return int2Str(sb.Length);"));
            StringAssert.Contains(ex.Message, "Length");
        }

        [TestMethod]
        public void ChainedStaticPropertyAndInstanceCall() =>
            Assert.AreEqual("2", Evaluate("System.Byte[] b = System.Text.Encoding::UTF8.GetBytes('ab'); return int2Str(b.Length);"));

        [TestMethod]
        public void NewObject_MemberCall_InExpression() =>
            Assert.AreEqual("xa", Evaluate("return 'x' + new System.Text.StringBuilder('a').ToString();"));

        [TestMethod]
        public void NewObject_MemberCall_AsAStatement() =>
            Assert.AreEqual("ok", Evaluate("new System.Text.StringBuilder().Append('a'); return 'ok';"));

        [TestMethod]
        public void FluentCalls_IgnoringTheResult() =>
            Assert.AreEqual("xyz", Evaluate("System.Text.StringBuilder sb = new System.Text.StringBuilder(); sb.Append('x'); sb.Append('y').Append('z'); return sb.ToString();"));

        [TestMethod]
        public void VoidMethod_CannotBeUsedAsAValue() =>
            Assert.IsTrue(ParseErrors("str s = ClrFixtures.Overloads::Boom();").Any());

        [TestMethod]
        public void OverloadsWithDifferentReturnTypes_CanBeUsedAsAValue() =>
            Assert.AreEqual("2.00", Evaluate("real r = 2.5; return strFmt('%1', System.Math::Round(r));"));
    }

    [TestClass]
    public class ClrStructTests
    {
        [TestMethod]
        public void Variable_WithoutInitialization_IsAnEmptyStruct() =>
            Assert.AreEqual("false", Evaluate("System.Threading.CancellationToken t; return any2Str(t.CanBeCanceled);"));

        [TestMethod]
        public void Variable_ChangingTheStruct_Stays() =>
            Assert.AreEqual("5", Evaluate("ClrFixtures.TestPoint p = new ClrFixtures.TestPoint(1, 2); p.X = 5; return int2Str(p.X);"));

        [TestMethod]
        public void Assignment_CopiesTheStruct() =>
            Assert.AreEqual("1 9", Evaluate("ClrFixtures.TestPoint p = new ClrFixtures.TestPoint(1, 2); ClrFixtures.TestPoint q = p; q.X = 9; return strFmt('%1 %2', p.X, q.X);"));

        [TestMethod]
        public void Parameter_CopiesTheStruct() =>
            Assert.AreEqual("1", Evaluate("void f(ClrFixtures.TestPoint q) { q.X = 9; } ClrFixtures.TestPoint p = new ClrFixtures.TestPoint(1, 2); f(p); return int2Str(p.X);"));

        [TestMethod]
        public void StructWithoutConstructorArguments() =>
            Assert.AreEqual("0", Evaluate("ClrFixtures.TestPoint p = new ClrFixtures.TestPoint(); return int2Str(p.X);"));

        [TestMethod]
        public void PassedToAMethod() =>
            Assert.AreEqual("1,2", Evaluate("ClrFixtures.TestPoint p = new ClrFixtures.TestPoint(1, 2); return ClrFixtures.Overloads::Struct(p);"));

        [TestMethod]
        public void MethodReturningAStruct() =>
            Assert.AreEqual("2", Evaluate("ClrFixtures.TestPoint p = new ClrFixtures.TestPoint(1, 2); return int2Str(p.Moved(1).X);"));

        [TestMethod]
        public void DateTime_ComparesWithOperators() =>
            Assert.AreEqual("true false", Evaluate("System.DateTime a = new System.DateTime(2020, 1, 1); System.DateTime b = new System.DateTime(2021, 1, 1); return strFmt('%1 %2', a < b, a == b);"));

        [TestMethod]
        public void XppDate_PassesToADateTimeParameter() =>
            Assert.AreEqual("2020", Evaluate(@"date d = 2\1\2020; System.DateTime t = System.DateTime::SpecifyKind(d, System.DateTimeKind::Utc); return int2Str(t.Year);"));

        [TestMethod]
        public void DateTime_ConvertsToXppDate() =>
            Assert.AreEqual("4/3/2020", Evaluate("date d = new System.DateTime(2020, 3, 4); return strFmt('%1', d);"));
    }

    [TestClass]
    public class ClrEnumTests
    {
        [TestMethod]
        public void Member() =>
            Assert.AreEqual("Monday 1 true", Evaluate("System.DayOfWeek d = System.DayOfWeek::Monday; int i = d; return strFmt('%1 %2 %3', d.ToString(), i, d == System.DayOfWeek::Monday);"));

        [TestMethod]
        public void IntToEnum() =>
            Assert.AreEqual("Tuesday", Evaluate("System.DayOfWeek d = 2; return d.ToString();"));

        [TestMethod]
        public void XppEnumToClrEnum() =>
            Assert.AreEqual("Monday", Evaluate("System.DayOfWeek d = TestXppEnum::Second; return d.ToString();"));

        [TestMethod]
        public void ClrEnumToXppEnum() =>
            Assert.AreEqual("Second", Evaluate("TestXppEnum e = System.DayOfWeek::Monday; return any2Str(e);"));

        [TestMethod]
        public void Flags_KeepTheType() =>
            Assert.AreEqual("A, B", Evaluate("var f = ClrFixtures.Options::A | ClrFixtures.Options::B; return f.ToString();"));

        [TestMethod]
        public void Flags_And() =>
            Assert.AreEqual("A", Evaluate("ClrFixtures.Options f = ClrFixtures.Options::A | ClrFixtures.Options::B; ClrFixtures.Options r = f & ClrFixtures.Options::A; return r.ToString();"));

        [TestMethod]
        public void InCondition() =>
            Assert.AreEqual("sun", Evaluate("System.DayOfWeek d = System.DayOfWeek::Sunday; return d ? 'other' : 'sun';"));

        [TestMethod]
        public void InArithmetic() =>
            Assert.AreEqual("3", Evaluate("return int2Str(System.DayOfWeek::Tuesday + 1);"));
    }

    [TestClass]
    public class ClrNumberTests
    {
        [TestMethod]
        public void DoubleResult_InArithmetic() =>
            Assert.AreEqual("5.00", Evaluate("real r = System.Math::Sqrt(16) + 1; return strFmt('%1', r);"));

        [TestMethod]
        public void AllNumericTypes_InArithmetic() =>
            Assert.AreEqual("9.50", Evaluate("System.Int16 a = 5; System.Single b = 1.5; System.UInt32 c = 3; real r = a + b + c; return strFmt('%1', r);"));

        [TestMethod]
        public void Byte_InArithmetic() =>
            Assert.AreEqual("6", Evaluate("System.Byte b = 5; int i = b + 1; return int2Str(i);"));

        [TestMethod]
        public void Double_InCondition() =>
            Assert.AreEqual("yes", Evaluate("System.Double d = 1.5; return d ? 'yes' : 'no';"));

        [TestMethod]
        public void Int32Zero_InCondition() =>
            Assert.AreEqual("false", Evaluate("System.Int32 i = 0; return i ? 'true' : 'false';"));

        [TestMethod]
        public void DeclaredClrNumber_KeepsItsType() =>
            Assert.AreEqual("Double", Evaluate("System.Double d = 1.5; System.Object o = d; return o.GetType().Name;"));

        [TestMethod]
        public void RealToClrNumberTypes() =>
            Assert.AreEqual("1.50 1.5 2.5", Evaluate("System.Decimal d = 1.5; System.Double x = d; System.Single y = 2.5; return strFmt('%1 %2 %3', d, x, y);"));
    }

    /// <summary>
    /// The patterns of a real X++ class that uses the Azure file share SDK.
    /// </summary>
    [TestClass]
    public class AzureHelperPatternTests
    {
        private const string Declarations = "System.Threading.CancellationToken tokenNull; System.Object objNull; ClrFixtures.Client client = new ClrFixtures.Client(); ";

        [TestMethod]
        public void ImplicitOperator_OnAssignment_GivesTheValue() =>
            Assert.AreEqual("false", Evaluate(Declarations + "boolean e = client.Exists(tokenNull); return any2Str(e);"));

        [TestMethod]
        public void ImplicitOperator_OnReassignment_GivesTheValue() =>
            Assert.AreEqual("false", Evaluate(Declarations + "boolean e = true; e = client.Exists(tokenNull); return any2Str(e);"));

        [TestMethod]
        public void ObjectInCondition_IsTrueWhenNotNull_EvenWithAnImplicitOperator() =>
            Assert.AreEqual("truthy", Evaluate(Declarations + "if (client.Exists(tokenNull)) { return 'truthy'; } return 'falsy';"));

        [TestMethod]
        public void NotOnObject_IsFalseWhenNotNull_EvenWithAnImplicitOperator() =>
            Assert.AreEqual("no", Evaluate(Declarations + "if (!client.Exists(tokenNull)) { return 'yes'; } return 'no';"));

        [TestMethod]
        public void ExplicitValue_InCondition() =>
            Assert.AreEqual("falsy", Evaluate(Declarations + "if (client.Exists(tokenNull).GetValue()) { return 'truthy'; } return 'falsy';"));

        [TestMethod]
        public void ImplicitOperator_ToAnotherClass() =>
            Assert.AreEqual("file.txt", Evaluate(Declarations + "ClrFixtures.Item i = client.Download(); return i.Name;"));

        [TestMethod]
        public void NullsInObjectVariables_GoToAnyClassParameter() =>
            Assert.AreEqual("four", Evaluate(Declarations + "return client.Create(1, objNull, objNull, tokenNull);"));

        [TestMethod]
        public void OverloadIsChosenByArgumentCount() =>
            Assert.AreEqual("five", Evaluate(Declarations + "return client.Create(1, objNull, objNull, objNull, tokenNull);"));

        [TestMethod]
        public void Enumerator_OfAPrivateIteratorClass() =>
            Assert.AreEqual("ab", Evaluate(Declarations +
                "var items = client.Items(); var en = items.GetEnumerator(); str r; " +
                "while (en.MoveNext()) { ClrFixtures.Item i = en.Current as ClrFixtures.Item; if (i != null && !i.IsDirectory) { r += i.Name; } } return r;"));

        [TestMethod]
        public void Enumerator_WithDeclaredInterfaceType() =>
            Assert.AreEqual("2", Evaluate(Declarations +
                "System.Collections.IEnumerator en = client.ItemsEnumerator(); int n; while (en.MoveNext()) { n++; } return int2Str(n);"));

        [TestMethod]
        public void StructConstructor_WithNullableParameter() =>
            Assert.AreEqual("0 10", Evaluate("System.Int64 len = 10; ClrFixtures.Range r = new ClrFixtures.Range(0, len); return strFmt('%1 %2', r.Offset, r.Length);"));

        [TestMethod]
        public void PropertySetter_FromIntToLong() =>
            Assert.AreEqual("0", Evaluate("System.IO.MemoryStream ms = new System.IO.MemoryStream(); ms.Position = 0; return int642Str(ms.Position);"));

        [TestMethod]
        public void StreamCopy() =>
            Assert.AreEqual("ab", Evaluate(
                "System.IO.MemoryStream input = new System.IO.MemoryStream(System.Text.Encoding::UTF8.GetBytes('ab')); " +
                "System.IO.MemoryStream copy = new System.IO.MemoryStream(); input.CopyTo(copy); copy.Position = 0; " +
                "using (System.IO.StreamReader rd = new System.IO.StreamReader(copy)) { return rd.ReadToEnd(); }"));

        [TestMethod]
        public void UsingDirectives_LikeTheClassHeader() =>
            Assert.AreEqual("file.txt", Evaluate("using ClrFixtures; Client c = new Client(); Item i = c.Download(); return i.Name;"));
    }

    [TestClass]
    public class ClrExceptionTests
    {
        [TestMethod]
        public void TypedCatch_GetsTheRealException() =>
            Assert.AreEqual("InvalidOperationException boom", Evaluate(
                "System.InvalidOperationException e; try { ClrFixtures.Overloads::Boom(); } catch (e) { return e.GetType().Name + ' ' + e.Message; } return 'none';"));

        [TestMethod]
        public void TypedCatch_OrderMatters() =>
            Assert.AreEqual("io", Evaluate(
                "System.IO.IOException io; System.Exception any; try { ClrFixtures.Overloads::BoomIo(); } catch (io) { return 'io'; } catch (any) { return 'any'; } return 'none';"));

        [TestMethod]
        public void TypedCatch_OfABaseClass() =>
            Assert.AreEqual("FileNotFoundException", Evaluate(
                "System.Exception e; try { ClrFixtures.Overloads::BoomIo(); } catch (e) { return e.GetType().Name; } return 'none';"));

        [TestMethod]
        public void TypedCatch_OfOtherType_DoesNotCatch() =>
            Assert.AreEqual("outer", Evaluate(
                "System.ArithmeticException a; try { try { ClrFixtures.Overloads::Boom(); } catch (a) { return 'wrong'; } } catch { return 'outer'; } return 'none';"));

        [TestMethod]
        public void TypedCatch_ThenFinally() =>
            Assert.AreEqual("cf", Evaluate(
                "str r; System.Exception e; try { ClrFixtures.Overloads::Boom(); } catch (e) { r += 'c'; } finally { r += 'f'; } return r;"));

        [TestMethod]
        public void ThrowClrException_CaughtByType() =>
            Assert.AreEqual("msg", Evaluate(
                "System.InvalidOperationException e; try { throw new System.InvalidOperationException('msg'); } catch (e) { return e.Message; } return 'none';"));

        [TestMethod]
        public void CatchClrError_CatchesClrExceptions() =>
            Assert.AreEqual("clr", Evaluate("try { ClrFixtures.Overloads::Boom(); } catch (Exception::CLRError) { return 'clr'; } return 'none';"));

        [TestMethod]
        public void CatchClrError_KeepsTheExceptionForGetLastException()
        {
            var host = new FakeXppHost();
            Evaluate("try { ClrFixtures.Overloads::Boom(); } catch (Exception::CLRError) { }", host);
            Assert.IsInstanceOfType(host.LastClrException, typeof(InvalidOperationException));
        }

        [TestMethod]
        public void CatchAll_AlsoKeepsTheException()
        {
            var host = new FakeXppHost();
            Evaluate("try { ClrFixtures.Overloads::Boom(); } catch { }", host);
            Assert.IsInstanceOfType(host.LastClrException, typeof(InvalidOperationException));
        }

        [TestMethod]
        public void CatchClrError_DoesNotCatchXppErrors() =>
            Assert.AreEqual("outer", Evaluate("try { try { throw Exception::Error; } catch (Exception::CLRError) { return 'clr'; } } catch { return 'outer'; } return 'none';"));

        [TestMethod]
        public void Uncaught_IsTheRealException() =>
            Assert.ThrowsException<InvalidOperationException>(() => Evaluate("ClrFixtures.Overloads::Boom();"));

        [TestMethod]
        public void Uncaught_KeepsTheStackTrace()
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(() => Evaluate("ClrFixtures.Overloads::Boom();"));
            StringAssert.Contains(ex.StackTrace, nameof(Overloads.Boom));
        }

        [TestMethod]
        public void Using_DisposesAtTheEnd()
        {
            Disposable.WasDisposed = false;
            Evaluate("using (ClrFixtures.Disposable d = new ClrFixtures.Disposable()) { }");
            Assert.IsTrue(Disposable.WasDisposed);
        }

        [TestMethod]
        public void Using_DisposesOnReturn()
        {
            Disposable.WasDisposed = false;
            Evaluate("using (ClrFixtures.Disposable d = new ClrFixtures.Disposable()) { return 1; }");
            Assert.IsTrue(Disposable.WasDisposed);
        }

        [TestMethod]
        public void Using_DisposesOnException_ThenPropagates()
        {
            Disposable.WasDisposed = false;
            Assert.ThrowsException<InvalidOperationException>(() => Evaluate("using (ClrFixtures.Disposable d = new ClrFixtures.Disposable()) { d.Fail(); }"));
            Assert.IsTrue(Disposable.WasDisposed);
        }

        [TestMethod]
        public void Using_DisposesOnBreak()
        {
            Disposable.WasDisposed = false;
            Evaluate("int i; for (i = 0; i < 2; i++) { using (ClrFixtures.Disposable d = new ClrFixtures.Disposable()) { break; } }");
            Assert.IsTrue(Disposable.WasDisposed);
        }

        [TestMethod]
        public void Using_WorksWithExplicitImplementation()
        {
            ExplicitDisposable.WasDisposed = false;
            Evaluate("using (ClrFixtures.ExplicitDisposable d = new ClrFixtures.ExplicitDisposable()) { }");
            Assert.IsTrue(ExplicitDisposable.WasDisposed);
        }

        [TestMethod]
        public void Using_OfSomethingNotDisposable_IsAnError() =>
            Assert.IsTrue(ParseErrors("using (System.Text.StringBuilder sb = new System.Text.StringBuilder()) { }").Any());
    }

    [TestClass]
    public class ClrValuesInXppTests
    {
        [TestMethod]
        public void NullChecks() =>
            Assert.AreEqual("true true true", Evaluate("System.Text.StringBuilder a, b = new System.Text.StringBuilder(); return strFmt('%1 %2 %3', a == null, !a, b != null);"));

        [TestMethod]
        public void ObjectInCondition() =>
            Assert.AreEqual("non", Evaluate("System.Text.StringBuilder a = new System.Text.StringBuilder(); return a ? 'non' : 'null';"));

        [TestMethod]
        public void NullInCondition() =>
            Assert.AreEqual("null", Evaluate("System.Text.StringBuilder a; return a ? 'non' : 'null';"));

        [TestMethod]
        public void InsideAContainer() =>
            Assert.AreEqual("1", Evaluate("container c = [new System.Text.StringBuilder()]; return int2Str(conLen(c));"));

        [TestMethod]
        public void InAnytype() =>
            Assert.AreEqual("any", Evaluate("anytype a = new System.Text.StringBuilder('any'); System.Text.StringBuilder sb = a; return sb.ToString();"));

        [TestMethod]
        public void InterfaceVariable() =>
            Assert.AreEqual("1", Evaluate("System.Collections.IList l = new System.Collections.ArrayList(); l.Add('a'); System.Collections.ICollection c = l; return int2Str(c.Count);"));

        [TestMethod]
        public void XppObject_AsAnObjectArgument() =>
            Assert.AreEqual("1", Evaluate("System.Collections.ArrayList l = new System.Collections.ArrayList(); l.Add(new TestXppClass()); return int2Str(l.Count);"));

        [TestMethod]
        public void XppObject_GoesBackToXpp() =>
            Assert.AreEqual("41", Evaluate("System.Collections.ArrayList l = new System.Collections.ArrayList(); l.Add(new TestXppClass()); TestXppClass c = l.get_Item(0) as TestXppClass; return int2Str(c.getNumber());"));

        [TestMethod]
        public void NullStringFromClr_IsEmptyInAStr() =>
            Assert.AreEqual("[] 0", Evaluate("System.String s = null; str t = s; return strFmt('[%1] %2', t, strLen(t));"));

        [TestMethod]
        public void ClrStringVariable_CanBeNull() =>
            Assert.AreEqual("true", Evaluate("System.String s; return any2Str(s == null);"));

        [TestMethod]
        public void StringsCompareWithoutCase_AlsoClrOnes() =>
            Assert.AreEqual("true", Evaluate("System.String a = 'A'; System.String b = 'a'; return any2Str(a == b);"));
    }

    [TestClass]
    public class ClrTypeResolverTests
    {
        private static ClrTypeResolver Resolver(params string[] references) => new ClrTypeResolver(() => references);

        [TestMethod]
        public void FindsLoadedTypes() =>
            Assert.AreEqual(typeof(System.IO.Path), Resolver().Find("System.IO.Path"));

        [TestMethod]
        public void IsCaseSensitive() =>
            Assert.IsNull(Resolver().Find("System.IO.PATH"));

        [TestMethod]
        public void FindsNestedTypes_WithADot() =>
            Assert.AreEqual(typeof(Statics.Nested), Resolver().Find("ClrFixtures.Statics.Nested"));

        [TestMethod]
        public void FindsGenericDefinitions() =>
            Assert.AreEqual(typeof(System.Collections.Generic.List<>), Resolver().Find("System.Collections.Generic.List", 1));

        [TestMethod]
        public void GenericArityMatters() =>
            Assert.IsNull(Resolver().Find("System.Collections.Generic.List", 2));

        [TestMethod]
        public void IgnoresNonPublicTypes() =>
            Assert.IsNull(Resolver().Find("System.Collections.Generic.ArraySortHelper", 1));

        [TestMethod]
        public void UnknownName_IsNull() =>
            Assert.IsNull(Resolver().Find("No.Such.Type"));

        [TestMethod]
        public void LoadsFrameworkAssembliesOnDemand() =>
            Assert.IsNotNull(Resolver().Find("System.Xml.Linq.XDocument"));

        [TestMethod]
        public void LoadsReferencedAssemblies_WhenTheNameDoesNotTell()
        {
            // EPPlus style: the namespace says nothing about the assembly. Microsoft.VisualBasic is not loaded by the tests.
            var type = Resolver("Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Find("Microsoft.VisualBasic.Strings");

            Assert.IsNotNull(type);
            Assert.AreEqual("Microsoft.VisualBasic.Strings", type.FullName);
        }

        [TestMethod]
        public void RootNamespaces_ComeFromAssembliesAndReferences()
        {
            var roots = Resolver("Some.Company.Library, Version=1.0.0.0", "lowercase.library").GetRootNamespaces();

            CollectionAssert.Contains(roots, "System");
            CollectionAssert.Contains(roots, "Some");
            CollectionAssert.DoesNotContain(roots, "lowercase");
            CollectionAssert.DoesNotContain(roots, "Dynamics");
        }

        [TestMethod]
        public void WrongReference_IsIgnored() =>
            Assert.IsNull(Resolver("No.Such.Assembly, Version=1.0.0.0").Find("No.Such.Assembly.Type"));

        [TestMethod]
        public void ReferencesAreOnlyAskedForOnce()
        {
            int calls = 0;
            var resolver = new ClrTypeResolver(() => { calls++; return new string[0]; });

            resolver.Find("No.Such.First");
            resolver.Find("No.Such.Second");

            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        public void Namespaces()
        {
            var resolver = Resolver();

            Assert.IsTrue(resolver.IsNamespace("System.IO"));
            Assert.IsTrue(resolver.IsNamespace("System"));
            Assert.IsFalse(resolver.IsNamespace("System.IO.Path"));
            Assert.IsFalse(resolver.IsNamespace("system.io"));
        }

        [TestMethod]
        public void NamespaceMembers()
        {
            var members = Resolver().GetNamespaceMembers("System").ToList();

            CollectionAssert.Contains(members, "IO.");
            CollectionAssert.Contains(members, "String");
        }

        [TestMethod]
        public void NamespaceMembers_WithoutGenericSuffix() =>
            CollectionAssert.Contains(Resolver().GetNamespaceMembers("System.Collections.Generic").ToList(), "List");
    }

    [TestClass]
    public class ClrConversionTests
    {
        [DataTestMethod]
        [DataRow(2.9, 2)]
        [DataRow(-2.9, -2)]
        [DataRow(3.5, 3)]
        public void RealToInt_Truncates(double value, int expected) =>
            Assert.AreEqual(expected, ClrConversions.Convert((decimal)value, typeof(int)));

        [TestMethod]
        public void BigInt64ToInt_WrapsAround() =>
            Assert.AreEqual(705032704, ClrConversions.Convert(5000000000L, typeof(int)));

        [TestMethod]
        public void NullToStruct_IsTheDefault() =>
            Assert.AreEqual(0, ClrConversions.Convert(null, typeof(int)));

        [TestMethod]
        public void NullToClass_IsNull() =>
            Assert.IsNull(ClrConversions.Convert(null, typeof(string)));

        [TestMethod]
        public void NoConversion_Fails() =>
            Assert.ThrowsException<InvalidCastException>(() => ClrConversions.Convert("text", typeof(int)));

        [TestMethod]
        public void ImplicitOperator() =>
            Assert.AreEqual(true, ClrConversions.Convert(new Response<bool>(true), typeof(bool)));

        [TestMethod]
        public void BooleanToNumber() =>
            Assert.AreEqual(1, ClrConversions.Convert(true, typeof(int)));

        [TestMethod]
        public void NumberToBoolean() =>
            Assert.AreEqual(true, ClrConversions.Convert(5, typeof(bool)));

        [TestMethod]
        public void StringToChar_OnlyWithOneCharacter()
        {
            Assert.AreEqual('a', ClrConversions.Convert("a", typeof(char)));
            Assert.AreEqual(ClrConversions.NoConversion, ClrConversions.GetCost("ab", null, typeof(char)));
        }

        [TestMethod]
        public void NumbersRank_WideningBeforeObject()
        {
            Assert.IsTrue(ClrConversions.GetCost(1, null, typeof(long)) < ClrConversions.GetCost(1, null, typeof(object)));
            Assert.IsTrue(ClrConversions.GetCost(1, null, typeof(int)) < ClrConversions.GetCost(1, null, typeof(long)));
            Assert.IsTrue(ClrConversions.GetCost(1, null, typeof(long)) < ClrConversions.GetCost(1, null, typeof(byte)));
        }

        [TestMethod]
        public void EnumRanks_ObjectBeforeInteger() =>
            Assert.IsTrue(ClrConversions.GetCost(DayOfWeek.Monday, null, typeof(object)) < ClrConversions.GetCost(DayOfWeek.Monday, null, typeof(int)));

        [TestMethod]
        public void CanConvert_ForTheParser()
        {
            Assert.IsTrue(ClrConversions.CanConvert(typeof(short), typeof(int)));
            Assert.IsTrue(ClrConversions.CanConvert(typeof(Response<bool>), typeof(bool)));
            Assert.IsTrue(ClrConversions.CanConvert(typeof(long?), typeof(long)));
            Assert.IsTrue(ClrConversions.CanConvert(typeof(object), typeof(string)));
            Assert.IsFalse(ClrConversions.CanConvert(typeof(string), typeof(int)));
            Assert.IsFalse(ClrConversions.CanConvert(typeof(System.Text.StringBuilder), typeof(int)));
        }
    }

    [TestClass]
    public class ClrBinderInformationTests
    {
        [TestMethod]
        public void ReturnType_WhenOverloadsAgree() =>
            Assert.AreEqual(typeof(string), ClrBinder.GetMethodReturnType(typeof(System.IO.Path), "Combine", true, 2));

        [TestMethod]
        public void ReturnType_IsUnknown_WhenOverloadsDiffer() =>
            Assert.IsNull(ClrBinder.GetMethodReturnType(typeof(Math), "Round", true, 1));

        [TestMethod]
        public void ReturnType_OfVoid() =>
            Assert.AreEqual(typeof(void), ClrBinder.GetMethodReturnType(typeof(Overloads), "Boom", true, 0));

        [TestMethod]
        public void ReturnType_ConsidersOptionalAndParams()
        {
            Assert.AreEqual(typeof(string), ClrBinder.GetMethodReturnType(typeof(Overloads), "Optional", true, 1));
            Assert.AreEqual(typeof(string), ClrBinder.GetMethodReturnType(typeof(Overloads), "Params", true, 5));
        }

        [TestMethod]
        public void Signatures_ShowAllOverloads() =>
            Assert.IsTrue(ClrBinder.GetMethodSignatures(typeof(Overloads), "Number", true).Length >= 4);

        [TestMethod]
        public void Signatures_AreWrittenAsInAScript()
        {
            string signature = ClrBinder.GetMethodSignatures(typeof(Overloads), "TryDouble", true).Single();

            StringAssert.Contains(signature, "System.Boolean TryDouble");
            StringAssert.Contains(signature, "byref System.Int32 result");
        }

        [TestMethod]
        public void FriendlyNames()
        {
            Assert.AreEqual("void", ClrBinder.FriendlyName(typeof(void)));
            Assert.AreEqual("System.String[]", ClrBinder.FriendlyName(typeof(string[])));
            Assert.AreEqual("System.Int32[,]", ClrBinder.FriendlyName(typeof(int[,])));
            Assert.AreEqual("System.Collections.Generic.List<System.String>", ClrBinder.FriendlyName(typeof(System.Collections.Generic.List<string>)));
        }

        [TestMethod]
        public void Members()
        {
            Assert.IsTrue(ClrBinder.HasMember(typeof(Members), "number", false));
            Assert.IsFalse(ClrBinder.HasMember(typeof(Members), "number", true));
            Assert.AreEqual(typeof(long), ClrBinder.GetMemberType(typeof(Members), "LongProperty", false));
            Assert.IsNull(ClrBinder.GetMemberType(typeof(Members), "Nothing", false));
        }

        [TestMethod]
        public void IndexersAreNotMembers() =>
            Assert.IsFalse(ClrBinder.HasMember(typeof(Members), "Item", false));

        [TestMethod]
        public void InterfaceMethods_AreFound_OnAPrivateClass()
        {
            object enumerator = new Client().ItemsEnumerator();

            Assert.AreEqual(true, ClrBinder.InvokeInstance(enumerator, "MoveNext", new object[0]));
            Assert.AreEqual("a", ((Item)ClrBinder.GetMember(enumerator, "Current")).Name);
        }

        [TestMethod]
        public void ObjectMethods_AreFound_OnAnInterface() =>
            Assert.IsTrue(ClrBinder.HasMethod(typeof(IDisposable), "ToString", false));
    }

    [TestClass]
    public class MethodHintTests
    {
        private static string Hint(string source, int line, int column, bool isMethodParameters = false)
        {
            var host = new FakeXppHost();
            var parser = new Parser.XppParser(new Lexer.XppLexer(source), host.Proxy);

            return parser.GetTokenMetadataAt(line, column, isMethodParameters)?.GetDisplayHtml();
        }

        [TestMethod]
        public void StaticMethod_ShowsAllOverloads()
        {
            string hint = Hint("str a = ClrFixtures.Overloads::Number(1);", 0, 33);

            StringAssert.Contains(hint, "System.Int32 value");
            StringAssert.Contains(hint, "System.Int64 value");
            StringAssert.Contains(hint, "<br/>");
        }

        [TestMethod]
        public void InstanceMethod()
        {
            string hint = Hint("System.Text.StringBuilder sb = new System.Text.StringBuilder();\nstr a = sb.ToString();", 1, 12);

            StringAssert.Contains(hint, "ToString");
        }

        [TestMethod]
        public void Constructor_ShowsTheConstructors()
        {
            string hint = Hint("System.Text.StringBuilder sb = new System.Text.StringBuilder('a');", 0, 61);

            StringAssert.Contains(hint, "StringBuilder(<b>System.String value</b>)");
        }

        [TestMethod]
        public void Parameter_IsInBold()
        {
            string hint = Hint("str a = ClrFixtures.Overloads::Optional(1, 'x');", 0, 41);

            StringAssert.Contains(hint, "<b>");
            StringAssert.Contains(hint, "[System.String b");
        }

        [TestMethod]
        public void Constructor_WithUsingShortName()
        {
            string hint = Hint("using System.Text;\nStringBuilder sb = new StringBuilder('a');", 1, 36);

            StringAssert.Contains(hint, "StringBuilder(<b>System.String value</b>)");
        }

        [TestMethod]
        public void Constructor_OfGenericType()
        {
            string hint = Hint("System.Collections.Generic.List<str> l = new System.Collections.Generic.List<str>(1);", 0, 82);

            StringAssert.Contains(hint, "capacity");
        }

        [TestMethod]
        public void Constructor_InsideArgumentOfAnother()
        {
            string hint = Hint("System.Text.StringBuilder sb = new System.Text.StringBuilder(new System.String('a', 1), 5);", 0, 86);

            StringAssert.Contains(hint, "StringBuilder(System.String value, <b>System.Int32 capacity</b>)");
        }

        [TestMethod]
        public void PointerOverloads_AreNotOffered()
        {
            string[] methods = Core.Clr.ClrBinder.GetMethodSignatures(typeof(System.Text.StringBuilder), "Append", false);
            string[] constructors = Core.Clr.ClrBinder.GetConstructorSignatures(typeof(string));

            Assert.IsTrue(methods.Length > 10);
            Assert.IsFalse(methods.Any(s => s.Contains("*")));
            Assert.IsTrue(constructors.Length > 1);
            Assert.IsFalse(constructors.Any(s => s.Contains("*")));
        }

        [TestMethod]
        public void GenericParameters_AreEncoded()
        {
            string html = new Parser.Metadata.ClrMethodTokenMetadata(new[] { "void F(System.Collections.Generic.Dictionary<System.String, System.Int32> a, System.Int32 b)" }).GetDisplayHtml(1);

            StringAssert.Contains(html, "&lt;System.String, System.Int32&gt; a");
            StringAssert.Contains(html, "<b>System.Int32 b</b>");
        }
    }

    [TestClass]
    public class AutocompleteTests
    {
        private static Parser.Metadata.CompletionCollection Complete(string source, Parser.Metadata.AutoCompletionPurpose purpose)
        {
            var host = new FakeXppHost();
            int cursor = source.LastIndexOf('|');

            // As the editor sends them: from zero, and the column is where the cursor is
            int line = source.Take(cursor).Count(c => c == '\n');
            int column = cursor - source.Substring(0, cursor).LastIndexOf('\n') - 1;

            return new Parser.Metadata.CompletionProvider(host.Proxy).GetCompletions(null, source.Replace("|", ""), line, column, purpose);
        }

        private static System.Collections.Generic.List<string> Values(Parser.Metadata.CompletionCollection collection) =>
            collection.Select(c => c.Value).ToList();

        [TestMethod]
        public void Namespace_InAnExpression()
        {
            var values = Values(Complete("System.|", Parser.Metadata.AutoCompletionPurpose.InstanceMembers));

            CollectionAssert.Contains(values, "IO");
            CollectionAssert.Contains(values, "String");
        }

        [TestMethod]
        public void Namespace_Deeper() =>
            CollectionAssert.Contains(Values(Complete("System.IO.|", Parser.Metadata.AutoCompletionPurpose.InstanceMembers)), "Path");

        [TestMethod]
        public void Namespace_AfterNew() =>
            CollectionAssert.Contains(Values(Complete("var a = new System.Text.|", Parser.Metadata.AutoCompletionPurpose.InstanceMembers)), "StringBuilder");

        [TestMethod]
        public void StaticMembers() =>
            CollectionAssert.Contains(Values(Complete("System.IO.Path::|", Parser.Metadata.AutoCompletionPurpose.StaticMembers)), "Combine");

        [TestMethod]
        public void EnumMembers() =>
            CollectionAssert.Contains(Values(Complete("var a = System.DayOfWeek::|", Parser.Metadata.AutoCompletionPurpose.StaticMembers)), "Friday");

        [TestMethod]
        public void InstanceMembers()
        {
            var values = Values(Complete("System.Text.StringBuilder sb = new System.Text.StringBuilder();\nsb.|", Parser.Metadata.AutoCompletionPurpose.InstanceMembers));

            CollectionAssert.Contains(values, "Append");
            CollectionAssert.Contains(values, "Length");
        }

        [TestMethod]
        public void MembersOfACallResult() =>
            CollectionAssert.Contains(Values(Complete("var a = System.Text.Encoding::UTF8.|", Parser.Metadata.AutoCompletionPurpose.InstanceMembers)), "GetBytes");

        [TestMethod]
        public void VariableNamedLikeANamespace_IsNotANamespace() =>
            Assert.AreEqual(0, Values(Complete("int System;\nSystem.|", Parser.Metadata.AutoCompletionPurpose.InstanceMembers)).Count(v => v == "IO"));
    }
}
