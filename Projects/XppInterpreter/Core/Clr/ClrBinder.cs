using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace XppInterpreter.Core.Clr
{
    /// <summary>
    /// Calls .NET members from a script: finds the best overload for the values, as the X++ compiler does for their types.
    /// Member names are not case sensitive, as in X++.
    /// </summary>
    public static class ClrBinder
    {
        private const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        private const BindingFlags InstanceMembers = BindingFlags.Public | BindingFlags.Instance;

        private static readonly ConcurrentDictionary<Tuple<Type, string, bool>, MethodBase[]> Methods = new ConcurrentDictionary<Tuple<Type, string, bool>, MethodBase[]>();

        #region Calls
        /// <param name="arguments">The arguments. The values of the byref parameters are written back here.</param>
        /// <param name="argumentTypes">The declared type of each argument, null when it's not known. Optional.</param>
        public static object InvokeStatic(Type type, string name, object[] arguments, Type[] argumentTypes = null) =>
            Invoke(GetMethods(type, name, true), null, type, name, arguments, argumentTypes);

        /// <param name="arguments">The arguments. The values of the byref parameters are written back here.</param>
        /// <param name="argumentTypes">The declared type of each argument, null when it's not known. Optional.</param>
        public static object InvokeInstance(object instance, string name, object[] arguments, Type[] argumentTypes = null)
        {
            if (instance is null)
            {
                throw new NullReferenceException($"Method '{name}' was called on a null object.");
            }

            Type type = instance.GetType();

            return Invoke(GetMethods(type, name, false), instance, type, name, arguments, argumentTypes);
        }

        public static object Construct(Type type, object[] arguments, Type[] argumentTypes = null)
        {
            if (type.IsArray)
            {
                int[] lengths = arguments.Select(a => (int)ClrConversions.Convert(a, typeof(int))).ToArray();

                return Array.CreateInstance(type.GetElementType(), lengths);
            }

            // A struct has no constructor without parameters
            if (type.IsValueType && arguments.Length == 0)
            {
                return Activator.CreateInstance(type);
            }

            MethodBase[] constructors = GetConstructors(type);

            return Invoke(constructors, null, type, ".ctor", arguments, argumentTypes);
        }

        private static object Invoke(MethodBase[] candidates, object instance, Type type, string name, object[] arguments, Type[] argumentTypes)
        {
            var binding = SelectOverload(candidates, arguments.Length, (i, target) => ClrConversions.GetCost(arguments[i], argumentTypes?[i], target));

            if (binding is null)
            {
                throw new MissingMethodException(DescribeMissingOverload(candidates, type, name, arguments));
            }

            object[] invokeArguments = binding.BuildArguments(arguments);
            object result;

            try
            {
                result = binding.Method is ConstructorInfo constructor
                    ? constructor.Invoke(invokeArguments)
                    : binding.Method.Invoke(instance, invokeArguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // A script catches the real exception
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }

            binding.WriteBackByRefArguments(invokeArguments, arguments);

            return result;
        }
        #endregion

        #region Overload resolution
        private sealed class Binding
        {
            public MethodBase Method;
            public ParameterInfo[] Parameters;
            public bool ExpandedParams;
            public int Cost;

            public object[] BuildArguments(object[] arguments)
            {
                object[] result = new object[Parameters.Length];
                int fixedCount = ExpandedParams ? Parameters.Length - 1 : Parameters.Length;

                for (int i = 0; i < fixedCount; i++)
                {
                    if (i < arguments.Length)
                    {
                        Type type = ParameterType(Parameters[i]);

                        // What an out parameter receives is not used
                        result[i] = Parameters[i].IsOut && Parameters[i].ParameterType.IsByRef
                            ? DefaultOf(type)
                            : ClrConversions.Convert(arguments[i], type);
                    }
                    else
                    {
                        result[i] = Parameters[i].HasDefaultValue ? Parameters[i].DefaultValue : DefaultOf(ParameterType(Parameters[i]));
                    }
                }

                if (ExpandedParams)
                {
                    Type elementType = Parameters[fixedCount].ParameterType.GetElementType();
                    int count = Math.Max(0, arguments.Length - fixedCount);
                    Array values = Array.CreateInstance(elementType, count);

                    for (int i = 0; i < count; i++)
                    {
                        values.SetValue(ClrConversions.Convert(arguments[fixedCount + i], elementType), i);
                    }

                    result[fixedCount] = values;
                }

                return result;
            }

            public void WriteBackByRefArguments(object[] invokeArguments, object[] arguments)
            {
                int count = Math.Min(arguments.Length, ExpandedParams ? Parameters.Length - 1 : Parameters.Length);

                for (int i = 0; i < count; i++)
                {
                    if (Parameters[i].ParameterType.IsByRef)
                    {
                        arguments[i] = invokeArguments[i];
                    }
                }
            }
        }

        private static object DefaultOf(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        private static Type ParameterType(ParameterInfo parameter) =>
            parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType() : parameter.ParameterType;

        private static Binding SelectOverload(MethodBase[] candidates, int argumentCount, Func<int, Type, int> costOf)
        {
            Binding best = null;

            foreach (var candidate in candidates)
            {
                var parameters = candidate.GetParameters();

                var binding = BindNormalForm(candidate, parameters, argumentCount, costOf)
                    ?? BindExpandedForm(candidate, parameters, argumentCount, costOf);

                if (binding != null && (best is null || binding.Cost < best.Cost))
                {
                    best = binding;
                }
            }

            return best;
        }

        private static Binding BindNormalForm(MethodBase method, ParameterInfo[] parameters, int argumentCount, Func<int, Type, int> costOf)
        {
            if (argumentCount > parameters.Length)
            {
                return null;
            }

            int cost = 0;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i >= argumentCount)
                {
                    if (!parameters[i].IsOptional)
                    {
                        return null;
                    }

                    // A method that needs no default value is a better match
                    cost++;
                    continue;
                }

                if (parameters[i].IsOut && parameters[i].ParameterType.IsByRef)
                {
                    continue;
                }

                int argumentCost = costOf(i, ParameterType(parameters[i]));

                if (argumentCost == ClrConversions.NoConversion)
                {
                    return null;
                }

                cost += argumentCost;
            }

            return new Binding { Method = method, Parameters = parameters, Cost = cost };
        }

        private static Binding BindExpandedForm(MethodBase method, ParameterInfo[] parameters, int argumentCount, Func<int, Type, int> costOf)
        {
            if (parameters.Length == 0 || !parameters.Last().IsDefined(typeof(ParamArrayAttribute), false) || argumentCount < parameters.Length - 1)
            {
                return null;
            }

            int fixedCount = parameters.Length - 1;

            // A method with exactly that number of parameters is a better match
            int cost = 2;

            for (int i = 0; i < argumentCount; i++)
            {
                Type target = i < fixedCount ? ParameterType(parameters[i]) : parameters[fixedCount].ParameterType.GetElementType();

                int argumentCost = costOf(i, target);

                if (argumentCost == ClrConversions.NoConversion)
                {
                    return null;
                }

                cost += argumentCost;
            }

            return new Binding { Method = method, Parameters = parameters, ExpandedParams = true, Cost = cost };
        }

        private static string DescribeMissingOverload(MethodBase[] candidates, Type type, string name, object[] arguments)
        {
            string typeName = type.FullName ?? type.Name;
            string given = string.Join(", ", arguments.Select(a => a?.GetType().Name ?? "null"));

            if (candidates.Length == 0)
            {
                return $"Type '{typeName}' does not have a method called '{name}'.";
            }

            string available = string.Join("; ", candidates.Take(10).Select(c => Signature(c)));

            return $"No overload of '{typeName}.{candidates[0].Name}' accepts the arguments ({given}). Available: {available}";
        }
        #endregion

        #region Members
        public static object GetMember(object instance, string name)
        {
            if (instance is null)
            {
                throw new NullReferenceException($"Member '{name}' was read on a null object.");
            }

            Type type = instance.GetType();

            return ReadMember(FindMember(type, name, false), instance, type, name);
        }

        /// <summary>
        /// Writes a property or a field of an object. A struct is changed where it is boxed.
        /// </summary>
        public static void SetMember(object instance, string name, object value)
        {
            if (instance is null)
            {
                throw new NullReferenceException($"Member '{name}' was written on a null object.");
            }

            Type type = instance.GetType();

            WriteMember(FindMember(type, name, false), instance, type, name, value);
        }

        public static object GetStaticMember(Type type, string name) =>
            ReadMember(FindMember(type, name, true), null, type, name);

        public static void SetStaticMember(Type type, string name, object value) =>
            WriteMember(FindMember(type, name, true), null, type, name, value);

        private static object ReadMember(MemberInfo member, object instance, Type type, string name)
        {
            try
            {
                switch (member)
                {
                    case PropertyInfo property: return property.GetValue(instance);
                    case FieldInfo field: return field.GetValue(instance);
                }
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }

            throw new MissingMemberException($"Type '{type.FullName}' does not have a field or a property called '{name}'.");
        }

        private static void WriteMember(MemberInfo member, object instance, Type type, string name, object value)
        {
            try
            {
                switch (member)
                {
                    case PropertyInfo property:
                        property.SetValue(instance, ClrConversions.Convert(value, property.PropertyType));
                        return;
                    case FieldInfo field:
                        field.SetValue(instance, ClrConversions.Convert(value, field.FieldType));
                        return;
                }
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }

            throw new MissingMemberException($"Type '{type.FullName}' does not have a field or a property called '{name}'.");
        }

        private static MemberInfo FindMember(Type type, string name, bool isStatic)
        {
            BindingFlags flags = isStatic ? StaticMembers : InstanceMembers;

            foreach (var current in TypeAndInterfaces(type, isStatic))
            {
                var property = current.GetProperties(flags).FirstOrDefault(p =>
                    p.GetIndexParameters().Length == 0 && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                if (property != null)
                {
                    return property;
                }

                var field = current.GetFields(flags).FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        /// <summary>
        /// The object of a private class is only reachable through its interfaces: the enumerator of an iterator, for example.
        /// </summary>
        private static IEnumerable<Type> TypeAndInterfaces(Type type, bool isStatic)
        {
            yield return type;

            if (isStatic)
            {
                yield break;
            }

            foreach (var @interface in type.GetInterfaces())
            {
                yield return @interface;
            }

            if (type.IsInterface)
            {
                yield return typeof(object);
            }
        }

        private static MethodBase[] GetMethods(Type type, string name, bool isStatic)
        {
            return Methods.GetOrAdd(Tuple.Create(type, name.ToLowerInvariant(), isStatic), key =>
            {
                BindingFlags flags = isStatic ? StaticMembers : InstanceMembers;
                var signatures = new HashSet<string>();
                var methods = new List<MethodBase>();

                foreach (var current in TypeAndInterfaces(type, isStatic))
                {
                    foreach (var method in current.GetMethods(flags))
                    {
                        if (CanBeCalled(method)
                            && string.Equals(method.Name, name, StringComparison.OrdinalIgnoreCase)
                            && signatures.Add(SignatureKey(method)))
                        {
                            methods.Add(method);
                        }
                    }
                }

                return methods.ToArray();
            });
        }

        private static MethodBase[] GetConstructors(Type type) =>
            type.GetConstructors(InstanceMembers).Where(CanBeCalled).Cast<MethodBase>().ToArray();

        // Generic methods and pointers can't be used from X++
        private static bool CanBeCalled(MethodBase method) =>
            !method.ContainsGenericParameters && !method.GetParameters().Any(p => p.ParameterType.IsPointer);

        private static string SignatureKey(MethodBase method) =>
            method.Name + "(" + string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name)) + ")";
        #endregion

        #region Information for the parser and the editor
        public static bool HasMethod(Type type, string name, bool isStatic) =>
            GetMethods(type, name, isStatic).Length > 0;

        public static bool HasMember(Type type, string name, bool isStatic) =>
            FindMember(type, name, isStatic) != null;

        public static Type GetMemberType(Type type, string name, bool isStatic)
        {
            switch (FindMember(type, name, isStatic))
            {
                case PropertyInfo property: return property.PropertyType;
                case FieldInfo field: return field.FieldType;
                default: return null;
            }
        }

        /// <summary>
        /// The type a call returns. Null when it depends on the overload, so nothing can be said before running.
        /// </summary>
        public static Type GetMethodReturnType(Type type, string name, bool isStatic, int argumentCount)
        {
            var returnTypes = GetMethods(type, name, isStatic)
                .Cast<MethodInfo>()
                .Where(m => AcceptsArgumentCount(m.GetParameters(), argumentCount))
                .Select(m => m.ReturnType)
                .Distinct()
                .Take(2)
                .ToArray();

            return returnTypes.Length == 1 ? returnTypes[0] : null;
        }

        /// <summary>
        /// The type a call returns when it depends on the overload: the types of the arguments choose it, as in the compiler.
        /// </summary>
        /// <returns>Null when the type of an argument is not known, or no overload accepts them</returns>
        public static Type GetMethodReturnType(Type type, string name, bool isStatic, Type[] argumentTypes)
        {
            if (argumentTypes.Contains(null))
            {
                return null;
            }

            var binding = SelectOverload(GetMethods(type, name, isStatic), argumentTypes.Length, (i, target) => ClrConversions.GetCost(argumentTypes[i], target));

            return (binding?.Method as MethodInfo)?.ReturnType;
        }

        private static bool AcceptsArgumentCount(ParameterInfo[] parameters, int argumentCount)
        {
            if (parameters.Length == argumentCount)
            {
                return true;
            }

            if (parameters.Length > 0 && parameters.Last().IsDefined(typeof(ParamArrayAttribute), false))
            {
                return argumentCount >= parameters.Length - 1;
            }

            return argumentCount < parameters.Length && parameters.Skip(argumentCount).All(p => p.IsOptional);
        }

        public static string[] GetMethodSignatures(Type type, string name, bool isStatic) =>
            GetMethods(type, name, isStatic).Select(m => Signature(m)).ToArray();

        public static string[] GetConstructorSignatures(Type type) =>
            GetConstructors(type).Select(c => Signature(c, type.Name)).ToArray();

        private static string Signature(MethodBase method, string nameOverride = null)
        {
            string parameters = string.Join(", ", method.GetParameters().Select(p =>
            {
                string text = FriendlyName(ParameterType(p)) + " " + p.Name;

                if (p.ParameterType.IsByRef) text = "byref " + text;
                if (p.IsOptional) text = "[" + text + "]";

                return text;
            }));

            string returnType = method is MethodInfo info ? FriendlyName(info.ReturnType) + " " : string.Empty;

            return $"{returnType}{nameOverride ?? method.Name}({parameters})";
        }

        public static string FriendlyName(Type type)
        {
            if (type == typeof(void)) return "void";

            if (type.IsArray)
            {
                return FriendlyName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            }

            string name = (type.FullName ?? type.Name).Replace('+', '.');

            if (type.IsGenericType)
            {
                int tick = name.IndexOf('`');

                return (tick < 0 ? name : name.Substring(0, tick))
                    + "<" + string.Join(", ", type.GetGenericArguments().Select(FriendlyName)) + ">";
            }

            return name;
        }

        public static IEnumerable<MethodInfo> GetPublicMethods(Type type, bool isStatic)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return TypeAndInterfaces(type, isStatic)
                .SelectMany(t => t.GetMethods(isStatic ? StaticMembers : InstanceMembers))
                .Where(m => !m.IsSpecialName && !m.ContainsGenericParameters && names.Add(m.Name));
        }

        public static IEnumerable<MemberInfo> GetPublicMembers(Type type, bool isStatic)
        {
            BindingFlags flags = isStatic ? StaticMembers : InstanceMembers;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return TypeAndInterfaces(type, isStatic)
                .SelectMany(t => t.GetProperties(flags).Where(p => p.GetIndexParameters().Length == 0).Cast<MemberInfo>().Concat(t.GetFields(flags)))
                .Where(m => !(m is FieldInfo field && field.IsSpecialName) && names.Add(m.Name));
        }
        #endregion
    }
}
