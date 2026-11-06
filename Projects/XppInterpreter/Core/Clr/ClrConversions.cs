using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace XppInterpreter.Core.Clr
{
    /// <summary>
    /// The conversions the X++ compiler applies between X++ and .NET values. The rules come from what compiled X++ does:
    /// see Projects\XppInterpreter.Tests\Conformance\conformance.txt
    /// </summary>
    public static class ClrConversions
    {
        public const int NoConversion = -1;

        // The cost of a conversion ranks the overloads of a method: the lowest wins
        private const int CostExact = 0;
        private const int CostNullable = 1;
        private const int CostBaseClass = 2;
        private const int CostInterface = 5;
        private const int CostDate = 3;
        private const int CostUserDefined = 7;
        private const int CostNarrowingNumber = 8;
        private const int CostFromBoolean = 10;
        private const int CostToBoolean = 12;
        private const int CostIntegerToEnum = 12;
        private const int CostEnumToEnum = 12;
        private const int CostToObject = 20;
        private const int CostEnumToInteger = 22; // Convert.ToString(NoYes::Yes) is "Yes": boxing wins over the integer value

        private static readonly ConcurrentDictionary<Tuple<Type, Type>, MethodInfo> ImplicitOperators = new ConcurrentDictionary<Tuple<Type, Type>, MethodInfo>();

        /// <param name="value">The value</param>
        /// <param name="staticType">The declared type of the variable holding the value, when it's known. It decides for null values.</param>
        /// <param name="target">The type of the parameter</param>
        /// <returns>The cost, or <see cref="NoConversion"/></returns>
        public static int GetCost(object value, Type staticType, Type target) =>
            value is null ? GetNullCost(staticType, target) : GetConversionCost(value.GetType(), target, value);

        /// <summary>
        /// The cost when only the type of the value is known, while a script is parsed.
        /// </summary>
        public static int GetCost(Type from, Type target) => GetConversionCost(from, target, null);

        private static int GetConversionCost(Type from, Type target, object value)
        {
            Type underlying = Nullable.GetUnderlyingType(target);

            if (underlying is null)
            {
                return GetTypeCost(from, target, value);
            }

            int cost = GetTypeCost(from, underlying, value);

            return cost == NoConversion ? NoConversion : cost + CostNullable;
        }

        private static int GetNullCost(Type staticType, Type target)
        {
            if (target.IsValueType && Nullable.GetUnderlyingType(target) is null)
            {
                return NoConversion;
            }

            if (staticType is null)
            {
                // Nothing tells the type: object takes anything, as a System.Object variable holding null would do
                return target == typeof(object) ? CostNullable : CostBaseClass;
            }

            if (target == staticType) return CostExact;
            if (target.IsAssignableFrom(staticType)) return CostNullable;
            if (staticType == typeof(object)) return CostBaseClass;

            return CostUserDefined;
        }

        private static int GetTypeCost(Type from, Type target, object value)
        {
            if (target == from) return CostExact;

            if (target == typeof(object)) return CostToObject;

            if (target.IsAssignableFrom(from))
            {
                return target.IsInterface ? CostInterface : CostBaseClass + InheritanceDistance(from, target);
            }

            if (IsNumber(from) && IsNumber(target))
            {
                return NumberCost(from, target);
            }

            if (from == typeof(bool))
            {
                return IsNumber(target) ? CostFromBoolean : UserDefinedCost(from, target);
            }

            if (target == typeof(bool))
            {
                return IsNumber(from) ? CostToBoolean : UserDefinedCost(from, target);
            }

            if (from.IsEnum)
            {
                if (IsNumber(target)) return CostEnumToInteger;
                if (target.IsEnum) return CostEnumToEnum;
            }

            if (target.IsEnum && IsNumber(from))
            {
                return CostIntegerToEnum;
            }

            if ((IsXppDateType(from) && target == typeof(DateTime)) || (from == typeof(DateTime) && IsXppDateType(target)))
            {
                return CostDate;
            }

            return UserDefinedCost(from, target);
        }

        private static int UserDefinedCost(Type from, Type target) =>
            GetImplicitOperator(from, target) != null ? CostUserDefined : NoConversion;

        private static int InheritanceDistance(Type from, Type target)
        {
            int distance = 0;

            for (Type current = from; current != null && current != target; current = current.BaseType)
            {
                distance++;
            }

            return Math.Min(distance, CostInterface - CostBaseClass);
        }

        // Widening conversions cost less, in the order the C# compiler prefers them
        private static int NumberCost(Type from, Type target)
        {
            int fromRank = NumberRank(from), targetRank = NumberRank(target);

            if (IsReal(from))
            {
                if (IsReal(target)) return from == typeof(decimal) || target == typeof(decimal) ? 4 : 3;

                return CostNarrowingNumber + 1;
            }

            if (IsReal(target))
            {
                return target == typeof(decimal) || target == typeof(double) ? 3 : 4;
            }

            if (targetRank > fromRank)
            {
                return targetRank - fromRank == 1 ? 1 : 2;
            }

            return CostNarrowingNumber;
        }

        private static int NumberRank(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return 1;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return 2;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return 3;
                default:
                    return 4;
            }
        }

        public static bool IsNumber(Type type)
        {
            if (type.IsEnum) return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsReal(Type type) =>
            type == typeof(decimal) || type == typeof(double) || type == typeof(float);

        /// <summary>
        /// The X++ date and utcdatetime: structs around a <see cref="DateTime"/>.
        /// </summary>
        public static bool IsXppDateType(Type type) =>
            type.IsValueType
            && Nullable.GetUnderlyingType(type) is null
            && type != typeof(DateTime)
            && type.GetProperty("Value")?.PropertyType == typeof(DateTime)
            && type.GetConstructor(new[] { typeof(DateTime) }) != null;

        /// <summary>
        /// Tells if the X++ compiler would accept an assignment between the types, when both are known when the script is parsed.
        /// </summary>
        public static bool CanConvert(Type from, Type target)
        {
            if (from == target || target.IsAssignableFrom(from) || from.IsAssignableFrom(target))
            {
                return true;
            }

            from = Nullable.GetUnderlyingType(from) ?? from;
            target = Nullable.GetUnderlyingType(target) ?? target;

            return from == target || GetTypeCost(from, target, null) != NoConversion;
        }

        /// <returns>False when there is no conversion</returns>
        public static bool TryConvert(object value, Type target, out object result)
        {
            result = value;

            if (value is null)
            {
                if (target.IsValueType && Nullable.GetUnderlyingType(target) is null)
                {
                    result = Activator.CreateInstance(target);
                }

                return true;
            }

            target = Nullable.GetUnderlyingType(target) ?? target;

            Type from = value.GetType();

            if (target.IsInstanceOfType(value))
            {
                return true;
            }

            if (target.IsEnum)
            {
                if (from.IsEnum || IsNumber(from))
                {
                    result = Enum.ToObject(target, ToIntegerValue(value));
                    return true;
                }

                return TryUserDefined(value, from, target, out result);
            }

            if (IsNumber(target))
            {
                if (from.IsEnum || IsNumber(from) || from == typeof(bool))
                {
                    result = ToNumber(value, target);
                    return true;
                }

                return TryUserDefined(value, from, target, out result);
            }

            if (target == typeof(bool))
            {
                if (from.IsEnum || IsNumber(from))
                {
                    result = ToIntegerValue(value) != 0;
                    return true;
                }

                return TryUserDefined(value, from, target, out result);
            }

            if (target == typeof(DateTime) && IsXppDateType(from))
            {
                result = from.GetProperty("Value").GetValue(value);
                return true;
            }

            if (from == typeof(DateTime) && IsXppDateType(target))
            {
                result = Activator.CreateInstance(target, value);
                return true;
            }

            return TryUserDefined(value, from, target, out result);
        }

        public static object Convert(object value, Type target)
        {
            if (TryConvert(value, target, out object result))
            {
                return result;
            }

            throw new InvalidCastException($"Cannot convert from type '{value?.GetType().FullName}' to type '{target.FullName}'.");
        }

        private static bool TryUserDefined(object value, Type from, Type target, out object result)
        {
            var conversion = GetImplicitOperator(from, target);

            if (conversion is null)
            {
                result = value;
                return false;
            }

            result = conversion.Invoke(null, new[] { value });
            return true;
        }

        private static MethodInfo GetImplicitOperator(Type from, Type target)
        {
            return ImplicitOperators.GetOrAdd(Tuple.Create(from, target), key =>
            {
                // The object can be of a private class derived from the one that has the operator, like the Response<T> of Azure
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

                return key.Item1.GetMethods(flags).Concat(key.Item2.GetMethods(flags)).FirstOrDefault(m =>
                    m.Name == "op_Implicit"
                    && key.Item2.IsAssignableFrom(m.ReturnType)
                    && m.GetParameters()[0].ParameterType.IsAssignableFrom(key.Item1));
            });
        }

        private static long ToIntegerValue(object value)
        {
            switch (value)
            {
                case decimal number: return (long)decimal.Truncate(number);
                case double number: return (long)Math.Truncate(number);
                case float number: return (long)Math.Truncate(number);
                case ulong number: return unchecked((long)number);
                case bool flag: return flag ? 1 : 0;
                default: return System.Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// A real becomes an integer by truncating, and a big integer wraps around, as in compiled X++.
        /// </summary>
        private static object ToNumber(object value, Type target)
        {
            if (IsReal(target))
            {
                if (value is bool flag) value = flag ? 1 : 0;
                if (value is Enum) value = ToIntegerValue(value);

                return System.Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
            }

            long integer = ToIntegerValue(value);

            unchecked
            {
                switch (Type.GetTypeCode(target))
                {
                    case TypeCode.SByte: return (sbyte)integer;
                    case TypeCode.Byte: return (byte)integer;
                    case TypeCode.Int16: return (short)integer;
                    case TypeCode.UInt16: return (ushort)integer;
                    case TypeCode.Int32: return (int)integer;
                    case TypeCode.UInt32: return (uint)integer;
                    case TypeCode.UInt64: return (ulong)integer;
                    default: return integer;
                }
            }
        }
    }
}
