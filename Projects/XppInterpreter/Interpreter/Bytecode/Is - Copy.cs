using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class As : IInstruction
    {
        public string OperationCode => $"AS {TypeName}";
        public string TypeName { get; }

        public As(string typeName)
        {
            TypeName = typeName;
        }

        public void Execute(RuntimeContext context)
        {
            var value = context.Stack.Pop();

            context.Stack.Push(context.Proxy.Casting.As(value, TypeName));
        }
    }
}
