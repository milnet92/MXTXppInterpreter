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
        void SetStaticProperty(string className, string propertyName, object value);
        object GetStaticProperty(string className, string functionName);
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
        Type GetFieldReturnType(Type caller, string fieldName, bool includeNonPublic = false);
        bool TypeHasMethod(Type type, string methodName);
        bool TypeHasProperty(Type type, string propertyName, bool includeNonPublic = false);
        bool EnumHasMember(string enumName, string memberName);
        Type GetDateType();
        Type GetUtcDateTimeType();

        /// <summary>
        /// Tells if the type comes from X++: a class, a table or an enumeration. Any other type is called as .NET.
        /// </summary>
        bool IsXppType(Type type);

        string[] GetReferencedAssemblies();

        /// <summary>
        /// The value of a variable that X++ code has without declaring it: appl, infolog or classFactory.
        /// </summary>
        object GetSystemVariable(string name);

        object GetFieldValueById(object record, int fieldId);

        void SetFieldValueById(object record, int fieldId, object value);

        /// <returns>The text, or null when there is no library with that name</returns>
        string GetMacroLibrary(string name);
    }
}
