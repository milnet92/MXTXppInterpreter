using System;
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

        // Every global function call of a script looks its method up by name: the methods of a type don't change while the process runs
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Tuple<Type, string>, MethodInfo> MethodsByName =
            new System.Collections.Concurrent.ConcurrentDictionary<Tuple<Type, string>, MethodInfo>();

        private static MethodInfo FindMethod(Type type, string methodName) =>
            type is null ? null : MethodsByName.GetOrAdd(Tuple.Create(type, methodName.ToLowerInvariant()),
                key => key.Item1.GetMethods().FirstOrDefault(m => m.Name.ToLowerInvariant() == key.Item2));

        public static bool TypeHasMethod(Type type, string methodName) => FindMethod(type, methodName) != null;

        public static bool TypeHasProperty(Type type, string propertyName, bool includeNonPublic = false)
        {
            if (type is null) return false;
            return type.GetProperties(GetBindingFlags(includeNonPublic)).Any(p => p.Name?.ToLower() == propertyName.ToLower());
        }

        public static bool TypeHasField(Type type, string fieldName, bool includeNonPublic)
        {
            if (type is null) return false;
            return type.GetFields(GetBindingFlags(includeNonPublic)).Any(f => f.Name?.ToLower() == fieldName.ToLower());
        }

        public static string GetMethodInvariantName(Type type, string methodName) => FindMethod(type, methodName)?.Name;

        public static PropertyInfo GetProperty(Type type, string propertyName, bool includeNonPublic = false)
        {
            return TypeHasProperty(type, propertyName, includeNonPublic)
                ? type.GetProperties(GetBindingFlags(includeNonPublic)).FirstOrDefault(p => p.Name.ToLowerInvariant() == propertyName.ToLowerInvariant())
                : null;
        }

        public static FieldInfo GetField(Type type, string fieldName, bool includeNonPublic = false)
        {
            return TypeHasField(type, fieldName, includeNonPublic)
                ? type.GetFields(GetBindingFlags(includeNonPublic)).FirstOrDefault(f => f.Name.ToLowerInvariant() == fieldName.ToLowerInvariant())
                : null;
        }

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
            var methods = type.GetMethods().Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase)).ToArray();

            if (methods.Length != 1)
            {
                return arguments;
            }

            var parameters = methods[0].GetParameters();
            int fixedCount = parameters.Length - 1;

            if (parameters.Length == 0 || parameters[fixedCount].ParameterType != typeof(object[]) || arguments.Length < fixedCount)
            {
                return arguments;
            }

            if (arguments.Length == parameters.Length && (arguments[fixedCount] is null || arguments[fixedCount] is object[]))
            {
                return arguments;
            }

            object[] packed = new object[parameters.Length];

            Array.Copy(arguments, packed, fixedCount);
            packed[fixedCount] = arguments.Skip(fixedCount).ToArray();

            return packed;
        }

        public static bool IsDelegateType(Type type)
        {
            if (type is null || type.BaseType is null) return false;

            return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
        }
    }
}
