using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
namespace ConsoleApp2
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
