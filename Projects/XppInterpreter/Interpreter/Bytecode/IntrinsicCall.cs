using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Interpreter.Bytecode
{
    class IntrinsicCall : Call
    {
        public override string OperationCode => $"CALL {Name} {NArgs}";

        public IntrinsicCall(string funcName, int nArgs, bool alloc) : base(funcName, nArgs, alloc) { }

        public override object MakeCall(RuntimeContext context, object[] arguments)
        {
            return XppProxyHelper.CallIntrinsicFunction(context.Proxy.Intrinsic, Name, arguments);
        }
    }
}
