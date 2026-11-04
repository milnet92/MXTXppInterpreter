using System;
using System.Collections.Generic;

namespace XppInterpreter.Parser
{
    /// <summary>
    /// The variables that X++ code has without declaring them.
    /// </summary>
    internal static class SystemVariables
    {
        private static readonly Dictionary<string, string> TypeNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["appl"] = "Application",
            ["infolog"] = "Info",
            ["classFactory"] = "ClassFactory"
        };

        public static bool IsSystemVariable(string name) => TypeNames.ContainsKey(name);

        public static string GetTypeName(string name) => TypeNames.TryGetValue(name, out string typeName) ? typeName : null;
    }
}
