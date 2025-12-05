using System;
using System.Linq;
using System.Reflection;

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
            if (type is null) return false;

            return type.GetMethods().Any(m => m.Name?.ToLower() == methodName.ToLower());
        }

        public static bool TypeHasProperty(Type type, string propertyName, bool includeNonPublic = false)
        {
            if (type is null) return false;
            return type.GetProperties(GetBindingFlags(includeNonPublic)).Any(p => p.Name?.ToLower() == propertyName.ToLower());
        }

        public static bool TypeHasField(Type type, string fieldName, bool includeNonPublic)
        {
            if (type is null) return false;
            return type.GetFields(GetBindingFlags(includeNonPublic)).Any(f => f.Name?.ToLower() == fieldName.ToLower());
        }

        public static string GetMethodInvariantName(Type type, string methodName)
        {
            return TypeHasMethod(type, methodName)
                ? type.GetMethods().FirstOrDefault(m => m.Name.ToLowerInvariant() == methodName.ToLowerInvariant()).Name
                : null;
        }

        public static PropertyInfo GetProperty(Type type, string propertyName, bool includeNonPublic = false)
        {
            return TypeHasProperty(type, propertyName, includeNonPublic)
                ? type.GetProperties(GetBindingFlags(includeNonPublic)).FirstOrDefault(p => p.Name.ToLowerInvariant() == propertyName.ToLowerInvariant())
                : null;
        }

        public static FieldInfo GetField(Type type, string fieldName, bool includeNonPublic = false)
        {
            return TypeHasField(type, fieldName, includeNonPublic)
                ? type.GetFields(GetBindingFlags(includeNonPublic)).FirstOrDefault(f => f.Name.ToLowerInvariant() == fieldName.ToLowerInvariant())
                : null;
        }

        private static BindingFlags GetBindingFlags(bool includeNonPublic)
        {
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

            if (includeNonPublic)
            {
                bindingFlags |= BindingFlags.NonPublic;
            }

            return bindingFlags;
        }

        public static MethodInfo GetMethod(Type type, string methodName)
        {
            string invariantMethodName = GetMethodInvariantName(type, methodName);

            return !string.IsNullOrEmpty(invariantMethodName)
                ? type.GetMethods().FirstOrDefault(m => m.Name.ToLowerInvariant() == methodName.ToLowerInvariant())
                : null;
        }

        public static bool TypeImplementsInterface(Type type, Type interfaceType)
        {
            if (type is null || interfaceType is null) return false;
            
            TypeInfo typeInfo = type as TypeInfo;
            return typeInfo != null && typeInfo.ImplementedInterfaces.Contains(interfaceType);
        }

        public static bool IsDelegateType(Type type)
        {
            if (type is null || type.BaseType is null) return false;

            return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
        }
    }
}
