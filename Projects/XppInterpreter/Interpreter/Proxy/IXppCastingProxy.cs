using System;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppCastingProxy
    {
        bool ToBoolean(object value);
        object GetDefaultValueForType(string typeName);
        object CreateDynamicArray(string typeName);
        object CreateFixedArray(string typeName, int size);
        object GetArrayIndexValue(object array, int index);
        void SetArrayIndexValue(object array, int index, object value);
        Type GetSystemTypeFromTypeName(string typeName);
        bool Is(object value, string typeName);
        object As(object value, string typeName);
        bool ImplicitConversionExists(Type from, Type to);
        bool IsReferenceType(Type type);
        object Cast(object value, Type toType);

    }
}
