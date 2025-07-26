using System;
using System.Linq;
using System.Reflection;

namespace XppInterpreter.Interpreter.Bytecode.Events
{
    public static class DelegateHelper
    {
        public static MethodInfo GetMethodToExecute(object instance, params Type[] types)
        {
            if (types.Length == 0)
            {
                return instance?.GetType().GetMethods().FirstOrDefault(m => m.Name == "Execute" && m.GetParameters().Count() == 0);
            }

            var definition = instance?.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.GetGenericMethodDefinition().GetParameters().Count() == types.Length);
            return definition.MakeGenericMethod(types);
        }
    }
}
