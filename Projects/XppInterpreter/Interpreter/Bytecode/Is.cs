using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class Is : IInstruction
    {
        public string OperationCode => $"IS {TypeName}";
        public string TypeName { get; }

        public Is(string typeName)
        {
            TypeName = typeName;
        }

        public void Execute(RuntimeContext context)
        {
            var value = context.Stack.Pop();

            context.Stack.Push(context.Proxy.Casting.Is(value, TypeName));
        }
    }
}
