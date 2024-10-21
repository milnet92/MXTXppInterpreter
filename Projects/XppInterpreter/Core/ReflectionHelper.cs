﻿using System;
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

        public static bool TypeHasProperty(Type type, string propertyName)
        {
            if (type is null) return false;
            return type.GetProperties().Any(p => p.Name?.ToLower() == propertyName.ToLower());
        }

        public static bool TypeHasField(Type type, string fieldName)
        {
            if (type is null) return false;
            return type.GetFields().Any(f => f.Name?.ToLower() == fieldName.ToLower());
        }

        public static string GetMethodInvariantName(Type type, string methodName)
        {
            return TypeHasMethod(type, methodName)
                ? type.GetMethods().FirstOrDefault(m => m.Name.ToLowerInvariant() == methodName.ToLowerInvariant()).Name
                : null;
        }

        public static PropertyInfo GetProperty(Type type, string propertyName)
        {
            return TypeHasProperty(type, propertyName)
                ? type.GetProperties().FirstOrDefault(p => p.Name.ToLowerInvariant() == propertyName.ToLowerInvariant())
                : null;
        }

        public static FieldInfo GetField(Type type, string fieldName)
        {
            return TypeHasField(type, fieldName)
                ? type.GetFields().FirstOrDefault(f => f.Name.ToLowerInvariant() == fieldName.ToLowerInvariant())
                : null;
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

        public static MethodInfo FindMethodCore(Type type, string methodName, object[] parameters, bool isStatic)
        {
            MethodInfo method = null;
            if (type is null) return null;

            BindingFlags bindingAttr = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            var methods = type.GetMethods(bindingAttr).Where(m => m.Name.ToLowerInvariant() == methodName.ToLowerInvariant());

            foreach (var m in methods) 
            {
                var parametersInfo = m.GetParameters();
                if (parametersInfo.Length != parameters.Length) continue;

                bool found = true;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i] == null)
                    {
                        if (parametersInfo[i].ParameterType.IsValueType)
                        {
                            found = false;
                            break;
                        }
                    }
                    else if (!parametersInfo[i].ParameterType.IsAssignableFrom(parameters[i].GetType()))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    method = m;
                    break;
                }
            }

            return method;
        }
    }
}
