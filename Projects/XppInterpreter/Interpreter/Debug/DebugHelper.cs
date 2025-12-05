using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace XppInterpreter.Interpreter.Debug
{
    public static class DebugHelper
    {
        public static string GetEdtArrayDebugDisplay(object array)
        {
            return $"Size = {array.GetType().GetProperty("Size").GetValue(array)}";
        }

        public static bool IsEnumerable(object instance)
        {
            if (instance is null)
            {
                return false;
            }

            return instance is IEnumerable;
        }

        public static string GetDebugDisplayValue(object instance)
        {
            if (instance is null)
            {
                return "null";
            }
            else if (instance is string str)
            {
                return $"\"{str}\"";
            }

            if (instance.GetType().Name.Contains("EdtArray"))
            {
                return GetEdtArrayDebugDisplay(instance);
            }
            else if (instance is object[] objArray) // Containers
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("[");

                int cnt;
                for (cnt = 0; cnt < 3 && cnt < objArray.Length; cnt++) // Max to 3 elements
                {
                    if (cnt != 0)
                    {
                        stringBuilder.Append(", ");
                    }

                    stringBuilder.Append(objArray[cnt]);
                }

                if (cnt < objArray.Length)
                {
                    stringBuilder.Append(" ... ");
                }

                stringBuilder.Append("]");

                return stringBuilder.ToString();
            }

            return instance.ToString();
        }
    }
}
