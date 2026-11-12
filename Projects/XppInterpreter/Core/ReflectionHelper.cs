using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace XppInterpreter.Core
{
    public static class ReflectionHelper
    {
        public static void SetEdtArrayIndexerValue(object indexedObject, int index, object value)
        {
            indexedObject.GetType().GetProperty("Item").SetValue(indexedObject, value, new object[] { index, null });
        }

        public static object GetEdtArrayIndexerValue(object indexedObject, int index)
        {
            return indexedObject.GetType().GetProperty("Item").GetValue(indexedObject, new object[] { index, null });
        }

        public static object MakeGenericInstance(Type type, Type genericType)
        {
            var makeType = type.MakeGenericType(genericType);
            return Activator.CreateInstance(makeType);
        }

        public static object MakeGenericInstance(Type type, Type genericType, object arg1)
        {
            var makeType = type.MakeGenericType(genericType);
            return Activator.CreateInstance(makeType, arg1);
        }

        public static object MakeGenericInstance(Type type, Type genericType, object arg1, object arg2)
        {
            var makeType = type.MakeGenericType(genericType);
            return Activator.CreateInstance(makeType, arg1, arg2);
        }

        // A script looks a member up by name on every call and on every read. The members of a type don't change while the
        // process runs, and a table has hundreds of them: these keep the answer, also when the type has no such member.
        // Names are compared without case and without culture: with a Turkish culture, "i" and "I" are different letters.
        private static readonly ConcurrentDictionary<MemberKey, MethodInfo> MethodsByName = new ConcurrentDictionary<MemberKey, MethodInfo>();
        private static readonly ConcurrentDictionary<MemberKey, PropertyInfo> PropertiesByName = new ConcurrentDictionary<MemberKey, PropertyInfo>();
        private static readonly ConcurrentDictionary<MemberKey, FieldInfo> FieldsByName = new ConcurrentDictionary<MemberKey, FieldInfo>();
        private static readonly ConcurrentDictionary<MemberKey, bool> DelegateFields = new ConcurrentDictionary<MemberKey, bool>();
        private static readonly ConcurrentDictionary<MemberKey, int> VariableArgumentPositions = new ConcurrentDictionary<MemberKey, int>();

        private const int NoVariableArguments = -1;

        private static bool IsNamed(MemberInfo member, string name) => string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase);

        private static MethodInfo FindMethod(Type type, string methodName) =>
            type is null || methodName is null ? null : MethodsByName.GetOrAdd(new MemberKey(type, methodName),
                key => key.Type.GetMethods().FirstOrDefault(m => IsNamed(m, key.Name)));

        public static bool TypeHasMethod(Type type, string methodName) => FindMethod(type, methodName) != null;

        public static bool TypeHasProperty(Type type, string propertyName, bool includeNonPublic = false) =>
            GetProperty(type, propertyName, includeNonPublic) != null;

        public static bool TypeHasField(Type type, string fieldName, bool includeNonPublic) =>
            GetField(type, fieldName, includeNonPublic) != null;

        public static string GetMethodInvariantName(Type type, string methodName) => FindMethod(type, methodName)?.Name;

        public static PropertyInfo GetProperty(Type type, string propertyName, bool includeNonPublic = false) =>
            type is null || propertyName is null ? null : PropertiesByName.GetOrAdd(new MemberKey(type, propertyName, includeNonPublic),
                key => key.Type.GetProperties(GetBindingFlags(key.Flag)).FirstOrDefault(p => IsNamed(p, key.Name)));

        public static FieldInfo GetField(Type type, string fieldName, bool includeNonPublic = false) =>
            type is null || fieldName is null ? null : FieldsByName.GetOrAdd(new MemberKey(type, fieldName, includeNonPublic),
                key => key.Type.GetFields(GetBindingFlags(key.Flag)).FirstOrDefault(f => IsNamed(f, key.Name)));

        /// <summary>
        /// Tells if the member is a delegate of an X++ class, which a script subscribes to instead of reading it.
        /// </summary>
        public static bool IsDelegateField(Type type, string fieldName) =>
            DelegateFields.GetOrAdd(new MemberKey(type, fieldName), key => IsDelegateType(GetField(key.Type, key.Name)?.FieldType));

        private static BindingFlags GetBindingFlags(bool includeNonPublic)
        {
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

            if (includeNonPublic)
            {
                bindingFlags |= BindingFlags.NonPublic;
            }

            return bindingFlags;
        }

        public static MethodInfo GetMethod(Type type, string methodName) => FindMethod(type, methodName);

        /// <summary>
        /// A kernel class whose constructor takes variable arguments, like Struct, receives them in an array.
        /// </summary>
        public static object[] PackConstructorArguments(Type type, object[] arguments)
        {
            var constructors = type.GetConstructors();

            if (constructors.Any(c => c.GetParameters().Length == arguments.Length && !IsVariableArguments(c))
                || !constructors.Any(IsVariableArguments)
                || (arguments.Length == 1 && (arguments[0] is null || arguments[0] is object[])))
            {
                return arguments;
            }

            return new object[] { arguments };
        }

        private static bool IsVariableArguments(ConstructorInfo constructor)
        {
            var parameters = constructor.GetParameters();

            return parameters.Length == 1 && parameters[0].ParameterType == typeof(object[]);
        }

        public static object CastToMap(object record, Type mapType)
        {
            Type common = record.GetType();

            while (common != null && common.FullName != "Microsoft.Dynamics.Ax.Xpp.Common")
            {
                common = common.BaseType;
            }

            if (common is null)
            {
                throw new InvalidCastException($"Cannot convert from type '{record.GetType().Name}' to type '{mapType.Name}'.");
            }

            MethodInfo cast = common.Assembly.GetType("Microsoft.Dynamics.Ax.Xpp.CastHelper").GetMethod("CastCommon").MakeGenericMethod(mapType);
            ParameterInfo[] parameters = cast.GetParameters();

            const int targetIsMap = 1, implicitContext = 0;

            try
            {
                return cast.Invoke(null, new[]
                {
                    record,
                    Enum.ToObject(parameters[1].ParameterType, targetIsMap),
                    Enum.ToObject(parameters[2].ParameterType, implicitContext)
                });
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        /// <summary>
        /// A kernel method with variable arguments, like DictClass.callStatic(name, ...), receives them in an array.
        /// </summary>
        public static object[] PackVariableArguments(Type type, string methodName, object[] arguments)
        {
            int fixedCount = VariableArgumentPositions.GetOrAdd(new MemberKey(type, methodName), key => FindVariableArguments(key.Type, key.Name));

            if (fixedCount == NoVariableArguments || arguments.Length < fixedCount)
            {
                return arguments;
            }

            if (arguments.Length == fixedCount + 1 && (arguments[fixedCount] is null || arguments[fixedCount] is object[]))
            {
                return arguments;
            }

            object[] packed = new object[fixedCount + 1];

            Array.Copy(arguments, packed, fixedCount);
            packed[fixedCount] = arguments.Skip(fixedCount).ToArray();

            return packed;
        }

        /// <returns>The position of the parameter that takes the variable arguments, when the type has one method with that name and it ends with one</returns>
        private static int FindVariableArguments(Type type, string methodName)
        {
            var methods = type.GetMethods().Where(m => IsNamed(m, methodName)).ToArray();

            if (methods.Length != 1)
            {
                return NoVariableArguments;
            }

            var parameters = methods[0].GetParameters();

            if (parameters.Length == 0 || parameters[parameters.Length - 1].ParameterType != typeof(object[]))
            {
                return NoVariableArguments;
            }

            return parameters.Length - 1;
        }

        public static bool IsDelegateType(Type type)
        {
            if (type is null || type.BaseType is null) return false;

            return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
        }
    }
}
