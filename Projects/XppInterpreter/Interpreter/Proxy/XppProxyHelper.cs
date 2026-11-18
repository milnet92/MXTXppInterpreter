using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XppInterpreter.Core;

namespace XppInterpreter.Interpreter.Proxy
{
    public static class XppProxyHelper
    {
        private static readonly Lazy<HashSet<string>> IntrinsicFunctions = new Lazy<HashSet<string>>(() => new HashSet<string>(
            typeof(IIntrinsicFunctionProvider).GetMethods()
                .Where(m => !m.GetCustomAttributes(typeof(IgnoreIntrinsicAttribute), true).Any())
                .Select(m => m.Name),
            StringComparer.OrdinalIgnoreCase));

        // A call like fieldNum(Table, Field) inside a loop looked its method up among all the methods of the provider every time
        private static readonly ConcurrentDictionary<MemberKey, MethodInfo> IntrinsicMethods = new ConcurrentDictionary<MemberKey, MethodInfo>();

        public static bool IsIntrinsicFunction(string name)
        {
            return IntrinsicFunctions.Value.Contains(name);
        }

        public static object CallIntrinsicFunction(IIntrinsicFunctionProvider provider, string name, object[] parameters)
        {
            var method = IntrinsicMethods.GetOrAdd(new MemberKey(provider.GetType(), name),
                key => key.Type.GetMethods().FirstOrDefault(m => string.Equals(m.Name, key.Name, StringComparison.OrdinalIgnoreCase)));

            return method.Invoke(provider, parameters);
        }
    }
}
