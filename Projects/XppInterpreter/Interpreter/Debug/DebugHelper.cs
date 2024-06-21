using System.Diagnostics;
using System.Linq;
using System.Text;
using Z.Expressions;

namespace XppInterpreter.Interpreter.Debug
{
    public static class DebugHelper
    {
        public static string GetDebugDisplayValue(object instance)
        {
            if (instance is null) return "null";

            if (instance.GetType().GetCustomAttributes(typeof(DebuggerDisplayAttribute), true).Any())
            {
                var displayAttribute = (DebuggerDisplayAttribute)instance.GetType().GetCustomAttributes(typeof(DebuggerDisplayAttribute), true).First();

                try
                {
                    return Eval.Execute<string>($"$\"{displayAttribute.Value}\"", instance);
                }
                catch
                {
                    // Do nothing
                }
            }
            else if (instance is object[] objArray) // Containers
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("[");

                int cnt = 0;
                for (cnt = 0; cnt < 3 && cnt < objArray.Length; cnt++) // Max to 3 elements
                {
                    if (cnt != 0)
                        stringBuilder.Append(", ");

                    stringBuilder.Append(objArray[cnt]);
                }

                if (cnt < objArray.Length)
                    stringBuilder.Append(" ... ");

                stringBuilder.Append("]");

                return stringBuilder.ToString();
            }

            return instance.ToString();
        }
    }
}
