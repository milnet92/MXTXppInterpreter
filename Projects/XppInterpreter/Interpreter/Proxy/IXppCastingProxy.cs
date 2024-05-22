using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
