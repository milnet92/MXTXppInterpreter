using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Proxy
{
    public static class XppProxyHelper
    {
        public static bool IsIntrinsicFunction(string name)
        {
            return typeof(IIntrinsicFunctionProvider).GetMethods().Any(m => m.Name.ToLowerInvariant() == name.ToLowerInvariant());
        }

        public static object CallIntrinsicFunction(IIntrinsicFunctionProvider provider, string name, object[] parameters)
        {
            return provider.GetType().GetMethod(name).Invoke(provider, parameters);
        }
    }
}
