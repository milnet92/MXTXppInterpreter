using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class ThrowException : IInstruction
    {
        public virtual string OperationCode => "THROW";

        public virtual void Execute(RuntimeContext context)
        {
            context.Proxy.Exceptions.Throw(context.Stack.Pop());
        }
    }
}
