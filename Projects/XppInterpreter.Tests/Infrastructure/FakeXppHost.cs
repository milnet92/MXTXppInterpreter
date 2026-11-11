using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using XppInterpreter.Core;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Interpreter.Query;

namespace XppInterpreter.Tests.Infrastructure
{
    /// <summary>
    /// Stands in for the X++ proxies (MXTXpp*Proxy), so the real lexer, parser, compiler and
    /// interpreter run outside D365. Casting and arithmetic mirror what the X++ proxies do.
    /// </summary>
    public sealed class FakeXppHost :
        IXppReflectionProxy,
        IXppCastingProxy,
        IXppBinaryOperationProxy,
        IXppUnaryOperationProxy,
        IXppDataAccessProxy,
        IXppExceptionsProxy,
        IXppQueryGenerationProxy
    {
        private sealed class Handle : IDisposable
        {
            private readonly Action _onDispose;

            public Handle(Action onDispose) { _onDispose = onDispose; }

            public void Dispose() => _onDispose();
        }

        public int CompanyHandlersOpened { get; private set; }
        public int CompanyHandlersOpen { get; private set; }
        public int UncheckedHandlersOpen { get; private set; }
        public int ContainerPacks { get; private set; }
        public int ContainerUnpacks { get; private set; }

        // How many times the metadata was asked. On an AOS each one is a lookup in the data dictionary.
        public int IsXppTypeCalls { get; private set; }
        public int IsEnumCalls { get; private set; }
        public int EnumValueCalls { get; private set; }
        public int IsStringEdtCalls { get; private set; }

        private static readonly Dictionary<string, Type> BuiltInTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["str"] = typeof(string),
            ["int"] = typeof(int),
            ["int64"] = typeof(long),
            ["real"] = typeof(decimal),
            ["boolean"] = typeof(bool),
            ["guid"] = typeof(Guid),
            ["container"] = typeof(object[]),
            ["anytype"] = typeof(object),
            ["date"] = typeof(FakeDate),
            ["utcdatetime"] = typeof(FakeUtcDateTime),
            ["timeofday"] = typeof(int),
            ["Global"] = typeof(FakeGlobal),
            ["PredefinedFunctions"] = typeof(FakePredefinedFunctions),
            ["TestXppClass"] = typeof(TestXppClass),
            ["TestMap"] = typeof(TestMap),
            ["Application"] = typeof(FakeApplication),
            ["TestXppEnum"] = typeof(TestXppEnum),
            ["TestCommon"] = typeof(TestCommon),
            ["Common"] = typeof(FakeCommon),
            ["Object"] = typeof(FakeXppObject),
            ["Exception"] = typeof(FakeExceptionEnum),
            ["FakeExceptionEnum"] = typeof(FakeExceptionEnum),
            ["DEVXppProbe"] = typeof(DEVXppProbe),
        };

        public List<string> Infos { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public int TtsLevelValue { get; private set; }
        public int TtsBeginCount { get; private set; }
        public int TtsCommitCount { get; private set; }
        public int TtsAbortCount { get; private set; }

        public XppProxy Proxy { get; }

        public FakeXppHost()
        {
            Proxy = new XppProxy(new FakeIntrinsicFunctionProvider(), this, this, this, this, this, this, this);
        }

        #region Reflection
        public object CreateInstance(string className, object[] parameters) =>
            Activator.CreateInstance(GetType(className), parameters);

        public object CallInstanceFunction(object instance, string functionName, object[] parameters) =>
            Invoke(instance.GetType(), instance, functionName, parameters);

        public object CallStaticFunction(string className, string functionName, object[] parameters) =>
            Invoke(GetType(className), null, functionName, parameters);

        private static object Invoke(Type type, object instance, string name, object[] parameters)
        {
            var method = type.GetMethods().FirstOrDefault(m =>
                string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase) &&
                m.GetParameters().Length == parameters.Length)
                ?? throw new MissingMethodException(type.Name, name);

            try
            {
                return method.Invoke(instance, parameters);
            }
            catch (TargetInvocationException ex)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        public object GetInstanceProperty(object instance, string propertyName)
        {
            if (instance is FakeCommon common) return common.GetField(propertyName);

            var type = instance.GetType();
            var property = ReflectionHelper.GetProperty(type, propertyName);
            if (property != null) return property.GetValue(instance);

            var field = ReflectionHelper.GetField(type, propertyName);
            if (field != null) return field.GetValue(instance);

            throw new Exception($"Invalid property {propertyName}");
        }

        public void SetInstanceProperty(object instance, string propertyName, object value)
        {
            if (instance is FakeCommon common)
            {
                common.SetField(propertyName, value);
                return;
            }

            var type = instance.GetType();
            var property = ReflectionHelper.GetProperty(type, propertyName);
            if (property != null)
            {
                property.SetValue(instance, value);
                return;
            }

            var field = ReflectionHelper.GetField(type, propertyName);
            if (field != null)
            {
                field.SetValue(instance, value);
                return;
            }

            throw new Exception($"Invalid property {propertyName}");
        }

        public void SetStaticProperty(string className, string propertyName, object value)
        {
            var type = GetType(className);
            var property = ReflectionHelper.GetProperty(type, propertyName);
            if (property != null)
            {
                property.SetValue(null, value);
                return;
            }

            ReflectionHelper.GetField(type, propertyName).SetValue(null, value);
        }

        public object GetStaticProperty(string className, string functionName)
        {
            var type = GetType(className);
            var property = ReflectionHelper.GetProperty(type, functionName);
            if (property != null) return property.GetValue(null);

            var field = ReflectionHelper.GetField(type, functionName);
            if (field != null) return field.GetValue(null);

            throw new Exception($"Invalid property {functionName}");
        }

        public bool IsInstantiable(string name) => GetSystemTypeFromTypeName(name) != null;

        public object CallGlobalOrPredefinedFunction(RuntimeContext context, string functionName, object[] parameters)
        {
            switch (functionName.ToLowerInvariant())
            {
                case "info":
                    Infos.Add(ToText(parameters[0]));
                    return null;
                case "warning":
                    Warnings.Add(ToText(parameters[0]));
                    return null;
                case "strfmt":
                    return StrFmt(parameters);
                case "any2str":
                    return ToText(parameters[0]);
                case "int2str":
                    return ToText(parameters[0] is bool flag ? (flag ? 1 : 0) : parameters[0]);
                case "strlen":
                    return ((string)parameters[0]).Length;
                case "conlen":
                    return ((object[])parameters[0]).Length;
                case "conpeek":
                    return ((object[])parameters[0])[(int)parameters[1] - 1];
                case "int642str":
                    return ToText(parameters[0]);
            }

            throw new Exception($"Function '{functionName}' was not found.");
        }

        public bool IsEnum(string name)
        {
            IsEnumCalls++;

            return GetSystemTypeFromTypeName(name)?.IsEnum ?? false;
        }

        public bool IsCommon(object instance) => instance is FakeCommon;
        public bool IsCommonType(Type type) => type != null && typeof(FakeCommon).IsAssignableFrom(type);
        public void ClearCommon(object common) => ((FakeCommon)common).Clear();
        public object GetEnumValue(string enumType, string enumValue)
        {
            EnumValueCalls++;

            return Enum.Parse(GetType(enumType), enumValue, true);
        }

        public string[] GetAllEnumValues(string enumType) => Enum.GetNames(GetType(enumType));
        public string GetMethodSyntax(string typeName, string methodName) => string.Empty;
        public string LabelIdToValue(string labelId, string languageId) => labelId == "@SYS1" ? "Time transactions" : labelId;
        public object GetSystemVariable(string name) => string.Equals(name, "appl", StringComparison.OrdinalIgnoreCase) ? new FakeApplication(this) : null;
        public object GetFieldValueById(object record, int fieldId) => ((FakeCommon)record).GetField("#" + fieldId);
        public void SetFieldValueById(object record, int fieldId, object value) => ((FakeCommon)record).SetField("#" + fieldId, value);
        public Func<string, string> MacroLibraries { get; set; }
        public string GetMacroLibrary(string name) => MacroLibraries?.Invoke(name);

        public Type GetMethodReturnType(Type typeName, string methodName) =>
            ReflectionHelper.GetMethod(typeName, methodName)?.ReturnType;

        public Type GetFieldReturnType(Type caller, string fieldName, bool includeNonPublic = false)
        {
            if (caller is null) return null;

            if (IsCommonType(caller)) return FakeCommon.GetFieldType(caller, fieldName);

            return ReflectionHelper.GetField(caller, fieldName, includeNonPublic)?.FieldType
                ?? ReflectionHelper.GetProperty(caller, fieldName, includeNonPublic)?.PropertyType;
        }

        public bool TypeHasMethod(Type type, string methodName) => ReflectionHelper.TypeHasMethod(type, methodName);

        public bool TypeHasProperty(Type type, string propertyName, bool includeNonPublic = false)
        {
            if (IsCommonType(type)) return FakeCommon.GetFieldType(type, propertyName) != null;

            return ReflectionHelper.TypeHasField(type, propertyName, includeNonPublic)
                || ReflectionHelper.TypeHasProperty(type, propertyName, includeNonPublic);
        }

        public bool EnumHasMember(string enumName, string memberName)
        {
            var type = GetSystemTypeFromTypeName(enumName);
            return type != null && type.IsEnum && Enum.GetNames(type).Contains(memberName, StringComparer.OrdinalIgnoreCase);
        }

        public Type GetDateType() => typeof(FakeDate);
        public Type GetUtcDateTimeType() => typeof(FakeUtcDateTime);

        public bool IsXppType(Type type)
        {
            IsXppTypeCalls++;

            return typeof(FakeXppObject).IsAssignableFrom(type)
                || typeof(FakeCommon).IsAssignableFrom(type)
                || type == typeof(TestXppEnum)
                || type == typeof(FakeExceptionEnum)
                || type == typeof(FakeXppArray);
        }

        public string[] GetReferencedAssemblies() => ReferencedAssemblies;

        public string[] ReferencedAssemblies { get; set; } = new string[0];
        #endregion

        #region Casting
        public bool ToBoolean(object value)
        {
            switch (value)
            {
                case null: return false;
                case bool b: return b;
                case string s: return s.Length > 0;
                case int i: return i != 0;
                case long l: return l != 0;
                case decimal d: return d != 0;
                case Enum e: return Convert.ToInt32(e) != 0;
                default: return true;
            }
        }

        public object GetDefaultValueForType(string typeName)
        {
            var type = GetSystemTypeFromTypeName(typeName);
            return type is null ? null : GetDefaultValueForSystemType(type);
        }

        public object GetDefaultValueForSystemType(Type type)
        {
            if (type == typeof(string)) return string.Empty;
            if (type == typeof(object[])) return new object[0];
            if (type == typeof(object)) return null;
            if (IsCommonType(type)) return Activator.CreateInstance(type);
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public object CreateDynamicArray(string typeName) => new FakeXppArray(GetSystemTypeFromTypeName(typeName), this);
        public object CreateFixedArray(string typeName, int size) => new FakeXppArray(GetSystemTypeFromTypeName(typeName), this);

        public object GetArrayIndexValue(object array, int index)
        {
            if (array is FakeXppArray xppArray) return xppArray[index];
            throw new Exception("The array indexing can only be applied to X++ array types.");
        }

        public void SetArrayIndexValue(object array, int index, object value)
        {
            if (array is FakeXppArray xppArray)
            {
                xppArray[index] = value;
                return;
            }

            throw new Exception("The array indexing can only be applied to X++ array types.");
        }

        public Type GetSystemTypeFromTypeName(string typeName) =>
            BuiltInTypes.TryGetValue(typeName, out var type) ? type : null;

        public bool Is(object value, string typeName)
        {
            var type = GetSystemTypeFromTypeName(typeName);
            return type != null && value != null && type.IsInstanceOfType(value);
        }

        public object As(object value, string typeName) => Is(value, typeName) ? value : null;

        // Same rules as MXTXppCastingProxy.ImplicitConversionExists
        public bool ImplicitConversionExists(Type from, Type to)
        {
            if (to is null) return false;
            if (from == to) return true;
            if (to.IsAssignableFrom(from) || from.IsAssignableFrom(to)) return true;
            if (to == typeof(object)) return true;

            bool IsNumeric(Type t) => t == typeof(int) || t == typeof(long) || t == typeof(decimal) || t == typeof(bool) || t.IsEnum;

            if (IsNumeric(from) && IsNumeric(to)) return true;

            return (to == typeof(FakeDate) && from == typeof(FakeUtcDateTime))
                || (to == typeof(FakeUtcDateTime) && from == typeof(FakeDate));
        }

        public bool IsReferenceType(Type type) =>
            type != null
            && type != typeof(string)
            && type != typeof(object)
            && type != typeof(object[])
            && !type.IsValueType
            && !IsCommonType(type);

        public object Cast(object value, Type toType)
        {
            if (value is FakeCommon && toType.IsSubclassOf(typeof(FakeCommon)) && !toType.IsInstanceOfType(value))
            {
                return Activator.CreateInstance(toType);
            }

            if (value != null)
            {
                var fromType = value.GetType();
                if (fromType == toType || toType.IsAssignableFrom(fromType) || fromType.IsAssignableFrom(toType)) return value;
            }

            if (toType.IsEnum) return Enum.ToObject(toType, Convert.ToInt32(value));
            if (toType == typeof(string)) return value?.ToString() ?? string.Empty;
            if (value is null) return GetDefaultValueForSystemType(toType);

            // In compiled X++ a real becomes an integer by truncating, and a big int64 wraps around
            if (value is decimal real && (toType == typeof(int) || toType == typeof(long)))
            {
                value = (long)decimal.Truncate(real);
            }

            if (toType == typeof(int)) return unchecked((int)Convert.ToInt64(value, CultureInfo.InvariantCulture));

            return Convert.ChangeType(value, toType, CultureInfo.InvariantCulture);
        }

        public Type GetArrayType(Type type) => typeof(FakeXppArray);
        public object CreateDate(int day, int month, int year) => new FakeDate(day, month, year);
        public bool IsStringEDT(string typeName)
        {
            IsStringEdtCalls++;

            return false;
        }

        public string TruncateEDTString(string value, string typeName) => value;

        public object ContainerPack(object value)
        {
            ContainerPacks++;
            return value;
        }

        public object ContainerUnpack(object value)
        {
            ContainerUnpacks++;
            return value;
        }
        #endregion

        #region Binary and unary operations
        private static bool IsReal(object o) => o is decimal || o is double || o is float;
        private static decimal Dec(object o) => o is Enum ? Convert.ToInt32(o) : Convert.ToDecimal(o, CultureInfo.InvariantCulture);

        private static object Numeric(object l, object r, Func<decimal, decimal, decimal> op)
        {
            decimal result = op(Dec(l), Dec(r));

            if (IsReal(l) || IsReal(r)) return result;
            if (l is long || r is long) return (long)result;
            return (int)result;
        }

        public object Add(object left, object right)
        {
            if (left is string || right is string) return ToText(left) + ToText(right);
            return Numeric(left, right, (a, b) => a + b);
        }

        public object Substract(object left, object right) => Numeric(left, right, (a, b) => a - b);
        public object Multiply(object left, object right) => Numeric(left, right, (a, b) => a * b);
        public object Divide(object left, object right) => Dec(left) / Dec(right);
        public object IntDivide(object left, object right)
        {
            // The result of div is an integer, whatever the operands are
            decimal result = decimal.Truncate(Dec(left) / Dec(right));

            return left is long || right is long ? (object)(long)result : (int)result;
        }
        public object LeftShift(object left, object right) => Convert.ToInt32(left) << Convert.ToInt32(right);
        public object RightShift(object left, object right) => Convert.ToInt32(left) >> Convert.ToInt32(right);
        public object BinaryAnd(object left, object right) => Convert.ToInt32(left) & Convert.ToInt32(right);
        public object BinaryOr(object left, object right) => Convert.ToInt32(left) | Convert.ToInt32(right);
        public object BinaryXOr(object left, object right) => Convert.ToInt32(left) ^ Convert.ToInt32(right);
        public object Mod(object left, object right) => Numeric(left, right, (a, b) => a % b);

        public bool Greater(object left, object right)
        {
            if (left is string l && right is string r) return string.Compare(l, r, StringComparison.OrdinalIgnoreCase) > 0;
            return Dec(left) > Dec(right);
        }

        public bool Like(object left, object right)
        {
            string pattern = "^" + System.Text.RegularExpressions.Regex.Escape((string)right).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";

            return System.Text.RegularExpressions.Regex.IsMatch((string)left, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        public bool AreEqual(object left, object right)
        {
            if (left is null || right is null) return ReferenceEquals(left, right);
            if (left is string l && right is string r) return string.Equals(l, r, StringComparison.OrdinalIgnoreCase);
            if (IsNumber(left) && IsNumber(right)) return Dec(left) == Dec(right);
            return left.Equals(right);
        }

        private static bool IsNumber(object o) => o is int || o is long || o is decimal || o is double || o is float || o is bool || o is Enum;

        public object Negate(object value) => !ToBoolean(value);
        #endregion

        #region Data access
        public void TtsBegin()
        {
            TtsBeginCount++;
            TtsLevelValue++;
        }

        public void TtsCommit()
        {
            TtsCommitCount++;
            TtsLevelValue--;
        }

        public void TtsAbort()
        {
            TtsAbortCount++;
            TtsLevelValue = 0;
        }

        public int TtsLevel() => TtsLevelValue;
        public IDisposable CreateChangeCompanyHandler(string datAreaId)
        {
            CompanyHandlersOpened++;
            CompanyHandlersOpen++;

            return new Handle(() => CompanyHandlersOpen--);
        }

        public IDisposable CreateUncheckedHandler(int uncheckedValue, string className, string methodName)
        {
            UncheckedHandlersOpen++;

            return new Handle(() => UncheckedHandlersOpen--);
        }
        public void Next(object common) => throw new NotSupportedException();
        public void Flush(string tableName) { }
        #endregion

        #region Exceptions
        public void Throw(object obj)
        {
            if (obj is Exception exception) throw exception;
            throw new FakeXppException(obj?.ToString());
        }

        public bool IsExceptionMember(Exception exception, string exceptionMember)
        {
            // CLRError is any exception that X++ did not throw, and compiled code keeps it for CLRInterop::getLastException()
            if (string.Equals(exceptionMember, "CLRError", StringComparison.OrdinalIgnoreCase) && !(exception is FakeXppException))
            {
                LastClrException = exception;
                return true;
            }

            return exception is FakeXppException xpp && string.Equals(xpp.Member, exceptionMember, StringComparison.OrdinalIgnoreCase);
        }

        public Exception LastClrException { get; private set; }

        public List<int> RetryCounts { get; } = new List<int>();

        public void SetRetryCount(int retryCount) => RetryCounts.Add(retryCount);
        #endregion

        public IQueryGenerator NewQueryGenerator() => throw new NotSupportedException();

        private Type GetType(string name) =>
            GetSystemTypeFromTypeName(name) ?? throw new Exception($"Type {name} was not found.");

        private static string StrFmt(object[] parameters)
        {
            string format = ToText(parameters[0]);
            for (int i = parameters.Length - 1; i >= 1; i--)
            {
                format = format.Replace("%" + i, ToText(parameters[i]));
            }
            return format;
        }

        public static string ToText(object value)
        {
            switch (value)
            {
                case null: return string.Empty;
                case bool b: return b ? "true" : "false";
                case decimal d: return d.ToString("0.00", CultureInfo.InvariantCulture);
                case Guid g: return g.ToString("B");
                // strFmt on the AOS prints a .NET enum as its number (conformance pL11, pL25); an X++ enum prints its name
                case Enum e when !(e is TestXppEnum) && !(e is FakeExceptionEnum): return Convert.ToInt32(e).ToString(CultureInfo.InvariantCulture);
                case IFormattable f: return f.ToString(null, CultureInfo.InvariantCulture);
                default: return value.ToString();
            }
        }
    }

    public sealed class FakeXppException : Exception
    {
        public string Member { get; }
        public FakeXppException(string member) : base("X++ exception " + member) { Member = member; }
    }

    /// <summary>Same shape as Microsoft.Dynamics.Ax.Xpp.AxShared.Date.</summary>
    public struct FakeDate
    {
        public DateTime Value { get; }
        public FakeDate(DateTime value) { Value = value; }
        public FakeDate(int day, int month, int year) { Value = new DateTime(year, month, day); }
        public override string ToString() => $"{Value.Day}/{Value.Month}/{Value.Year}";
    }

    /// <summary>Same shape as Microsoft.Dynamics.Ax.Xpp.AxShared.utcdatetime.</summary>
    public struct FakeUtcDateTime
    {
        public DateTime Value { get; }
        public FakeUtcDateTime(DateTime value) { Value = value; }
    }

    public sealed class FakeXppArray
    {
        private readonly Dictionary<int, object> _values = new Dictionary<int, object>();
        private readonly Type _elementType;
        private readonly FakeXppHost _host;

        public FakeXppArray(Type elementType, FakeXppHost host)
        {
            _elementType = elementType;
            _host = host;
        }

        public object this[int index]
        {
            get => _values.TryGetValue(index, out var value) ? value : _host.GetDefaultValueForSystemType(_elementType);
            set => _values[index] = value;
        }
    }

    /// <summary>The system variable appl.</summary>
    public class FakeApplication : FakeXppObject
    {
        private readonly FakeXppHost _host;

        public FakeApplication(FakeXppHost host) { _host = host; }

        public int ttsLevel() => _host.TtsLevel();
    }

    /// <summary>Same role as the D365 Global class: lets the parser infer return types.</summary>
    public static class FakeGlobal
    {
        public static void info(string text) { }
        public static void warning(string text) { }
        public static string strFmt(string format, params object[] arguments) => format;
        public static string int642Str(long value) => string.Empty;
        public static string any2Str(object value) => string.Empty;
        public static string int2Str(int value) => string.Empty;
        public static int strLen(string text) => 0;
        public static int conLen(object[] container) => 0;
        public static object conPeek(object[] container, int index) => null;
    }

    public static class FakePredefinedFunctions
    {
    }

    public enum FakeExceptionEnum
    {
        Info,
        Warning,
        Deadlock,
        Error,
        Internal,
        Break,
        Numeric,
        CLRError,
        UpdateConflict,
        UpdateConflictNotRecovered,
        DuplicateKeyException
    }

    /// <summary>
    /// The helpers the snippets of the conformance file call: the same ones as the class that ran them with the real compiler.
    /// </summary>
    public class DEVXppProbe : FakeXppObject
    {
        public static int counter;
        public static int staticValue;

        public static int nx() => ++counter;
        public static int fortyOne() => 41;
        public static DEVXppProbe make()
        {
            nx();
            return new DEVXppProbe();
        }

        public string show(int value) => $"arg={value} counter={counter}";
        public int num() => 41;
    }

    public enum TestXppEnum
    {
        First,
        Second,
        Third
    }

    /// <summary>Stands for a compiled X++ class.</summary>
    public class FakeXppObject
    {
    }

    public class TestXppClass : FakeXppObject
    {
        public static int Calls;
        public static int StaticValue;
        public int Number = 41;

        public TestXppClass() { }
        public TestXppClass(int number) { Number = number; }

        public int getNumber() => Number;
        public TestXppClass next() => new TestXppClass(Number + 1);
        public string name() => "xpp";
        public static int staticMethod() => 7;
        public static int nextCall() => ++Calls;
        public static void boom() => throw new FakeXppException("Error");
        public static int boomInt() => throw new FakeXppException("Error");
        public void noReturn() { }

        // Like DictClass.callStatic: a kernel method with variable arguments receives them in an array
        public string variableArguments(string name, object[] arguments) => name + arguments.Length;
    }

    /// <summary>Stands for a table buffer.</summary>
    public class FakeCommon
    {
        private readonly Dictionary<string, object> _fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public object GetField(string name) => _fields.TryGetValue(name, out var value) ? value : null;
        public FakeCommon orig() => this;
        public FakeCommon data() => this;
        public void SetField(string name, object value) => _fields[name] = value;
        public void Clear() => _fields.Clear();

        public static Type GetFieldType(Type table, string name) =>
            table.GetField(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)?.FieldType;
    }

    /// <summary>A table map.</summary>
    public class TestMap : FakeCommon
    {
        public string mapName() => "map";
        public string mapValue(int value) => "map " + value;
    }

    public class TestCommon : FakeCommon
    {
        public static string AccountNum = string.Empty;
    }
}
