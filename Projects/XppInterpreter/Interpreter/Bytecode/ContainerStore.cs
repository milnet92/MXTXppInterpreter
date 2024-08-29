using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class ContainerStore : Store
    {
        public int ContainerIndex { get; }
        public bool Last { get; }

        public ContainerStore(string name, int containerIndex, bool last, bool fromStack, bool top, bool isArray, string typeName, Type clrType) : base(name, fromStack, top, isArray, typeName, clrType)
        {
            ContainerIndex = containerIndex;
            Last = last;
        }

        internal override object GetValue(RuntimeContext context)
        {
            object value = Last ? context.Stack.Pop() : context.Stack.Peek();

            if (value is null || value.GetType() != typeof(object[]))
            {
                throw new Exception("Invalid container type for assignment.");
            }

            object[] container = value as object[];
            
            // Container index is 1 based
            if (container.Length < ContainerIndex)
            {
                return null;
            }

            return container[ContainerIndex - 1];
        }
    }
}
