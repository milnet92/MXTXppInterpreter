using System;
using System.Collections;
using System.Runtime.CompilerServices;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Lexer;

namespace XppInterpreter.Core.Clr
{
    /// <summary>
    /// How .NET values behave in X++ expressions. The X++ proxies only know X++ values, so .NET values are resolved here.
    /// </summary>
    public static class ClrValues
    {
        /// <summary>
        /// Tells if the value is an object of a .NET type, and not an X++ object or a value that both worlds share (str, int, real...).
        /// </summary>
        public static bool IsClrObject(object value, XppProxy proxy)
        {
            if (value is null) return false;

            System.Type type = value.GetType();

            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal || value is Guid || value is object[])
            {
                return false;
            }

            return !proxy.IsXppType(type) && !ClrConversions.IsXppDateType(type);
        }

        /// <summary>
        /// The value of a condition. A .NET object is true when it's not null, even if it has a conversion to boolean:
        /// <c>if (client.Exists())</c> is always true in compiled X++, while <c>boolean b = client.Exists();</c> gets the real value.
        /// </summary>
        public static bool ToBoolean(object value, XppProxy proxy)
        {
            if (value is null) return false;
            if (value is bool flag) return flag;
            if (IsClrObject(value, proxy)) return true;

            return proxy.Casting.ToBoolean(ToXppOperand(value, proxy));
        }

        public static object Negate(object value, XppProxy proxy)
        {
            if (value is null || IsClrObject(value, proxy))
            {
                return !ToBoolean(value, proxy);
            }

            return proxy.Unary.Negate(ToXppOperand(value, proxy));
        }

        /// <summary>
        /// X++ operators work on int, int64 and real: the other .NET numbers become the closest one.
        /// </summary>
        public static object ToXppOperand(object value, XppProxy proxy)
        {
            switch (value)
            {
                case byte number: return (int)number;
                case sbyte number: return (int)number;
                case short number: return (int)number;
                case ushort number: return (int)number;
                case uint number: return (long)number;
                case ulong number: return unchecked((long)number);
                case float number: return (decimal)number;
                case double number: return (decimal)number;
                case Enum enumeration when !proxy.IsXppType(enumeration.GetType()):
                    return Convert.ToInt32(enumeration);
                default: return value;
            }
        }

        /// <returns>False when the X++ proxies have to evaluate it</returns>
        public static bool TryEvaluate(TType operation, object left, object right, XppProxy proxy, out object result)
        {
            result = null;

            if (TryFloatingPoint(operation, left, right, out result))
            {
                return true;
            }

            switch (operation)
            {
                case TType.Equal:
                case TType.NotEqual:
                    if (!TryAreEqual(left, right, proxy, out bool equal)) return false;

                    result = operation == TType.Equal ? equal : !equal;
                    return true;

                case TType.Greater:
                case TType.GreaterOrEqual:
                case TType.Smaller:
                case TType.SmallerOrEqual:
                    if (!TryCompare(left, right, proxy, out int comparison)) return false;

                    result = operation == TType.Greater ? comparison > 0
                        : operation == TType.GreaterOrEqual ? comparison >= 0
                        : operation == TType.Smaller ? comparison < 0
                        : comparison <= 0;
                    return true;

                case TType.BinaryOr:
                case TType.BinaryAnd:
                case TType.BinaryXOr:
                    return TryFlags(operation, left, right, proxy, out result);

                default:
                    return false;
            }
        }

        /// <summary>
        /// A .NET double or float stays one: a real can't hold all their values.
        /// </summary>
        private static bool TryFloatingPoint(TType operation, object left, object right, out object result)
        {
            result = null;

            bool isFloatingPoint = left is double || left is float || right is double || right is float;

            if (!isFloatingPoint || !IsFloatingPointOperand(left) || !IsFloatingPointOperand(right))
            {
                return false;
            }

            double a = Convert.ToDouble(left), b = Convert.ToDouble(right);

            switch (operation)
            {
                case TType.Equal: result = a == b; return true;
                case TType.NotEqual: result = a != b; return true;
                case TType.Greater: result = a > b; return true;
                case TType.GreaterOrEqual: result = a >= b; return true;
                case TType.Smaller: result = a < b; return true;
                case TType.SmallerOrEqual: result = a <= b; return true;
                case TType.Plus: result = FloatingPointResult(a + b, left, right); return true;
                case TType.Minus: result = FloatingPointResult(a - b, left, right); return true;
                case TType.Star: result = FloatingPointResult(a * b, left, right); return true;
                case TType.Division: result = FloatingPointResult(a / b, left, right); return true;
                default: return false;
            }
        }

        private static bool IsFloatingPointOperand(object value) =>
            value is double || value is float || value is int || value is long;

        private static object FloatingPointResult(double value, object left, object right)
        {
            return left is double || right is double ? value : (object)(float)value;
        }

        private static bool TryAreEqual(object left, object right, XppProxy proxy, out bool equal)
        {
            equal = false;

            if (left is null && right is null)
            {
                equal = true;
                return true;
            }

            if (!IsClrObject(left, proxy) && !IsClrObject(right, proxy))
            {
                return false;
            }

            if (left is null || right is null)
            {
                return true;
            }

            equal = Equals(AlignWith(left, right), AlignWith(right, left));
            return true;
        }

        private static bool TryCompare(object left, object right, XppProxy proxy, out int comparison)
        {
            comparison = 0;

            if (!IsClrObject(left, proxy) && !IsClrObject(right, proxy))
            {
                return false;
            }

            left = AlignWith(left, right);
            right = AlignWith(right, left);

            if (left is IComparable && right != null && left.GetType() == right.GetType())
            {
                comparison = Comparer.Default.Compare(left, right);
                return true;
            }

            return false;
        }

        private static object AlignWith(object value, object other)
        {
            if (other is DateTime && value != null && ClrConversions.IsXppDateType(value.GetType()))
            {
                return ClrConversions.Convert(value, typeof(DateTime));
            }

            return value;
        }

        private static bool TryFlags(TType operation, object left, object right, XppProxy proxy, out object result)
        {
            result = null;

            if (!(left is Enum) || left.GetType() != right?.GetType() || proxy.IsXppType(left.GetType()))
            {
                return false;
            }

            long a = Convert.ToInt64(left), b = Convert.ToInt64(right);
            long value = operation == TType.BinaryOr ? a | b : operation == TType.BinaryAnd ? a & b : a ^ b;

            result = Enum.ToObject(left.GetType(), value);
            return true;
        }

        /// <summary>
        /// A variable that receives a struct has its own copy.
        /// </summary>
        public static object CopyIfStruct(object value) => RuntimeHelpers.GetObjectValue(value);

        /// <summary>
        /// The debugger reads the properties of an object when it pauses: these would block it, or start work the script didn't ask for.
        /// </summary>
        public static bool CanInspect(object instance, string propertyName)
        {
            if (propertyName == "Result" && instance is System.Threading.Tasks.Task task)
            {
                return task.IsCompleted;
            }

            System.Type type = instance.GetType();

            if (propertyName == "Value" && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                return (bool)type.GetProperty("IsValueCreated").GetValue(instance);
            }

            return true;
        }
    }
}
