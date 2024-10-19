using System;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppReflectionProxy
    {
        object CreateInstance(string nameSpace, string className, object[] parameters);
        object CallInstanceFunction(object instance, string functionName, object[] parameters);
        object CallStaticFunction(string nameSpace, string className, string functionName, object[] parameters);
        object GetInstanceProperty(object instance, string propertyName);
        void SetInstanceProperty(object instance, string propertyName, object value);
        object GetStaticProperty(string nameSpace, string className, string functionName);
        bool IsInstantiable(string name);
        object CallGlobalOrPredefinedFunction(RuntimeContext context, string functionName, object[] parameters);
        bool IsEnum(string name);
        bool IsCommon(object instance);
        bool IsCommonType(Type type);
        void ClearCommon(object common);
        object GetEnumValue(string enumType, string enumValue);
        string[] GetAllEnumValues(string enumType);
        string GetMethodSyntax(string typeName, string methodName);
        string LabelIdToValue(string labelId, string languageId);
        Type GetMethodReturnType(Type typeName, string methodName);
        Type GetFieldReturnType(Type caller, string fieldName);
        bool TypeHasMethod(Type type, string methodName);
        bool TypeHasProperty(Type type, string propertyName);
        bool EnumHasMember(string enumName, string memberName);
        bool IsAssemblyNamespace(string @namespace, bool _partialSearch);
        bool AssemblyHasType(string @namespace, string typeName);
        Type GetTypeFromNamespace(string @namespace, string typeName);
    }
}
