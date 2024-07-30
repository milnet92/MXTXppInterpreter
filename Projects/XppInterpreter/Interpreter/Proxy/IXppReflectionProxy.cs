using System;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppReflectionProxy
    {
        object CreateInstance(string className, object[] parameters);
        object CallInstanceFunction(object instance, string functionName, object[] parameters);
        object CallStaticFunction(string className, string functionName, object[] parameters);
        object GetInstanceProperty(object instance, string propertyName);
        void SetInstanceProperty(object instance, string propertyName, object value);
        object GetStaticProperty(string className, string functionName);
        bool IsInstantiable(string name);
        object CallGlobalOrPredefinedFunction(string functionName, object[] parameters);
        bool IsEnum(string name);
        bool IsCommon(object instance);
        bool IsCommonType(Type type);
        void ClearCommon(object common);
        object GetEnumValue(string enumType, string enumValue);
        string[] GetAllEnumValues(string enumType);
        string GetMethodSyntax(string typeName, string methodName);
    }
}
