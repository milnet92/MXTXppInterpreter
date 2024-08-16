using System.Linq;

namespace XppInterpreter.Interpreter.Proxy
{
    public static class XppProxyHelper
    {
        public static bool IsIntrinsicFunction(string name)
        {
            return typeof(IIntrinsicFunctionProvider).GetMethods()
                .Where(m => !m.GetCustomAttributes(typeof(IgnoreIntrinsicAttribute), true).Any()) 
                .Any(m => m.Name.ToLowerInvariant() == name.ToLowerInvariant());
        }

        public static object CallIntrinsicFunction(IIntrinsicFunctionProvider provider, string name, object[] parameters)
        {
            var method = provider.GetType().GetMethods().FirstOrDefault(m => m.Name.ToLowerInvariant() == name.ToLowerInvariant());

            return method.Invoke(provider, parameters);
        }
    }
}
