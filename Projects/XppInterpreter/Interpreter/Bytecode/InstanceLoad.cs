using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Core;

namespace XppInterpreter.Interpreter.Bytecode
{
    class InstanceLoad : Load
    {
        public InstanceLoad(string name, bool isArray) : base(name, isArray) { }

        public override object MakeLoad(RuntimeContext context)
        {
            object caller = context.Stack.Pop();
            return context.Proxy.Reflection.GetInstanceProperty(caller, Name);
        }

        public override object MakeLoadFromArray(RuntimeContext context, int index)
        {
            var array = MakeLoad(context);

            return context.Proxy.Casting.GetArrayIndexValue(array, index);
        }
    }
}
