using System;
using XppInterpreter.Lexer;

namespace XppInterpreter.Core
{
    /// <summary>
    /// The operators on two values of the simple types, computed here instead of in the X++ proxies: most operations of a
    /// script compare or add integers, and each one was one or two calls to X++.
    /// Only the operations whose result is the one of the compiler for certain are here. The conformance cases pN40 to pN50
    /// hold their edges: integers wrap around, also inside an anytype. Strings, dates, division, mod and sums of values of
    /// different types stay with the proxies.
    /// </summary>
    public static class SimpleOperands
    {
        private static readonly object True = true;
        private static readonly object False = false;

        /// <returns>False when the X++ proxies have to evaluate it</returns>
        public static bool TryEvaluate(TType operation, object left, object right, out object result)
        {
            result = null;

            if (left is int leftInt && right is int rightInt)
            {
                switch (operation)
                {
                    case TType.Plus:
                    case TType.PlusAssignment:
                    case TType.Increment:
                        result = unchecked(leftInt + rightInt);
                        return true;

                    case TType.Minus:
                    case TType.MinusAssignment:
                    case TType.Decrement:
                        result = unchecked(leftInt - rightInt);
                        return true;

                    case TType.Star:
                        result = unchecked(leftInt * rightInt);
                        return true;
                }

                return TryCompare(operation, leftInt.CompareTo(rightInt), out result);
            }

            // An int with an int64 gives an int64
            if (TryInteger(left, out long leftLong) && TryInteger(right, out long rightLong))
            {
                switch (operation)
                {
                    case TType.Plus:
                    case TType.PlusAssignment:
                    case TType.Increment:
                        result = unchecked(leftLong + rightLong);
                        return true;

                    case TType.Minus:
                    case TType.MinusAssignment:
                    case TType.Decrement:
                        result = unchecked(leftLong - rightLong);
                        return true;

                    case TType.Star:
                        result = unchecked(leftLong * rightLong);
                        return true;
                }

                return TryCompare(operation, leftLong.CompareTo(rightLong), out result);
            }

            if (TryCompareNumbers(left, right, out int comparison))
            {
                return TryCompare(operation, comparison, out result);
            }

            if ((operation == TType.Equal || operation == TType.NotEqual) && TryAreEqualValues(left, right, out bool equal))
            {
                result = equal == (operation == TType.Equal) ? True : False;
                return true;
            }

            return false;
        }

        /// <summary>
        /// The test of a case of a switch, and of ==.
        /// </summary>
        /// <returns>False when the X++ proxies have to compare them</returns>
        public static bool TryAreEqual(object left, object right, out bool equal)
        {
            if (TryCompareNumbers(left, right, out int comparison))
            {
                equal = comparison == 0;
                return true;
            }

            return TryAreEqualValues(left, right, out equal);
        }

        /// <summary>
        /// The value of a condition, when it is a number: zero is false.
        /// </summary>
        public static bool TryToBoolean(object value, out bool result)
        {
            switch (value)
            {
                case int number: result = number != 0; return true;
                case long number: result = number != 0; return true;
                case decimal number: result = number != 0; return true;
                default: result = false; return false;
            }
        }

        private static bool TryAreEqualValues(object left, object right, out bool equal)
        {
            equal = false;

            if (left is bool leftFlag && right is bool rightFlag)
            {
                equal = leftFlag == rightFlag;
                return true;
            }

            // Two values of one enumeration. An enumeration against a number, or against another one, is for the proxies.
            if (left is Enum && right != null && left.GetType() == right.GetType())
            {
                equal = left.Equals(right);
                return true;
            }

            return false;
        }

        /// <summary>
        /// int, int64 and real compare by their value, whatever the two types are.
        /// </summary>
        private static bool TryCompareNumbers(object left, object right, out int comparison)
        {
            comparison = 0;

            if (TryInteger(left, out long leftInteger))
            {
                if (TryInteger(right, out long rightInteger))
                {
                    comparison = leftInteger.CompareTo(rightInteger);
                    return true;
                }

                if (right is decimal rightReal)
                {
                    comparison = ((decimal)leftInteger).CompareTo(rightReal);
                    return true;
                }

                return false;
            }

            if (left is decimal leftReal)
            {
                if (right is decimal real)
                {
                    comparison = leftReal.CompareTo(real);
                    return true;
                }

                if (TryInteger(right, out long integer))
                {
                    comparison = leftReal.CompareTo((decimal)integer);
                    return true;
                }
            }

            return false;
        }

        private static bool TryInteger(object value, out long integer)
        {
            switch (value)
            {
                case int number: integer = number; return true;
                case long number: integer = number; return true;
                default: integer = 0; return false;
            }
        }

        private static bool TryCompare(TType operation, int comparison, out object result)
        {
            bool value;

            switch (operation)
            {
                case TType.Equal: value = comparison == 0; break;
                case TType.NotEqual: value = comparison != 0; break;
                case TType.Greater: value = comparison > 0; break;
                case TType.GreaterOrEqual: value = comparison >= 0; break;
                case TType.Smaller: value = comparison < 0; break;
                case TType.SmallerOrEqual: value = comparison <= 0; break;
                default:
                    result = null;
                    return false;
            }

            result = value ? True : False;
            return true;
        }
    }
}
