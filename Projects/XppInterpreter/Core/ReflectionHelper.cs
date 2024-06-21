using System;
using System.Linq;

namespace XppInterpreter.Core
{
    public static class ReflectionHelper
    {
        public static void SetEdtArrayIndexerValue(object indexedObject, int index, object value)
        {
            indexedObject.GetType().GetProperty("Item").SetValue(indexedObject, value, new object[] { index, null });
        }

        public static object GetEdtArrayIndexerValue(object indexedObject, int index)
        {
            return indexedObject.GetType().GetProperty("Item").GetValue(indexedObject, new object[] { index, null });
        }

        public static object MakeGenericInstance(Type type, Type genericType)
        {
            var makeType = type.MakeGenericType(genericType);
            return Activator.CreateInstance(makeType);
        }

        public static object MakeGenericInstance(Type type, Type genericType, object arg1)
        {
            var makeType = type.MakeGenericType(genericType);
            return Activator.CreateInstance(makeType, arg1);
        }

        public static object MakeGenericInstance(Type type, Type genericType, object arg1, object arg2)
        {
            var makeType = type.MakeGenericType(genericType);
            return Activator.CreateInstance(makeType, arg1, arg2);
        }

        public static bool TypeHasMethod(Type type, string methodName)
        {
            return type.GetMethods().Any(m => m.Name?.ToLower() == methodName.ToLower());
        }

        public static string GetMethodInvariantName(Type type, string methodName)
        {
            return TypeHasMethod(type, methodName)
                ? type.GetMethods().FirstOrDefault(m => m.Name.ToLowerInvariant() == methodName.ToLowerInvariant()).Name
                : null;
        }
    }
}
