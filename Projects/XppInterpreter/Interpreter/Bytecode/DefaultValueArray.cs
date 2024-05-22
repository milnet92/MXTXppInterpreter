using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class DefaultValueArray : DefaultValue
    {
        public override string OperationCode => $"PUSH_DEFAULT_A";
        public bool FixedSize { get; }
        
        public DefaultValueArray(string typeName, bool fixedSize) : base(typeName)
        {
            FixedSize = fixedSize;
        }

        public override void Execute(RuntimeContext context)
        {
            object defaultValue;

            if (FixedSize)
            {
                int size = (int)context.Stack.Pop();
                defaultValue = context.Proxy.Casting.CreateFixedArray(TypeName, size);
            }
            else
            { 
                defaultValue = context.Proxy.Casting.CreateDynamicArray(TypeName);
            }

            context.Stack.Push(defaultValue);
        }
    }
}
