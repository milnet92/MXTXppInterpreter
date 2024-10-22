using System;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppCastingProxy
    {
        bool ToBoolean(object value);
        object GetDefaultValueForType(string typeName, string @namespace);
        object CreateDynamicArray(string typeName, string @namespace);
        object CreateFixedArray(string typeName, string @namespace, int size);
        object GetArrayIndexValue(object array, int index);
        void SetArrayIndexValue(object array, int index, object value);
        Type GetSystemTypeFromTypeName(string typeName);
        bool Is(object value, string typeName, string @namespace);
        object As(object value, string typeName, string @namespace);
        bool ImplicitConversionExists(Type from, Type to);
        bool IsReferenceType(Type type);
        object Cast(object value, Type toType);
        Type GetArrayType(Type type);
        object CreateDate(int day, int month, int year);
    }
}
