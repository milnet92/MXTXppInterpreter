using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class InstanceFunctionCall : Call
    {
        public InstanceFunctionCall(string funcName, int nArgs, bool alloc) : base(funcName, nArgs, alloc, false) { }

        public override object MakeCall(RuntimeContext context, object[] arguments)
        {
            var instance = context.Stack.Pop();
            arguments = GetParameters(context);
            return context.Proxy.Reflection.CallInstanceFunction(instance, Name, arguments);
        }
    }
}
