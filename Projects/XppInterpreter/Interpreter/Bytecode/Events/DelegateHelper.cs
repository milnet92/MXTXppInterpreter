using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using XppInterpreter.Core;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter.Bytecode.Events
{
    public static class DelegateHelper
    {
        public static string[] GetDelegateFields(Type type)
        {
            return type.GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(f => ReflectionHelper.IsDelegateType(f.FieldType)).Select(d => d.Name).ToArray();
        }

        public static string[] GetEventNames(Type type)
        {
            return type.GetEvents(BindingFlags.Instance | BindingFlags.Public)
                .Select(d => d.Name).ToArray();
        }

        public static bool HasEvent(Type type, string eventName)
        {
            return type.GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public) != null;
        }

        public static Type[] GetDelegateParameterTypes(Type delegateType)
        {
            return delegateType.GetMethod("Invoke").GetParameters().Select(o => o.ParameterType).ToArray();
        }

        public static MethodInfo GetMethodToExecute(object instance, params Type[] types)
        {
            if (types.Length == 0)
            {
                return instance?.GetType().GetMethods().FirstOrDefault(m => m.Name == "Execute" && m.GetParameters().Count() == 0);
            }

            var definition = instance?.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == "Execute" && m.GetParameters().Count() > 0 && m.GetGenericMethodDefinition().GetParameters().Count() == types.Length);
            return definition.MakeGenericMethod(types);
        }
    }
}
