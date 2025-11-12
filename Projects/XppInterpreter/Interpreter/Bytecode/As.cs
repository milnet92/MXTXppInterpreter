using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class As : IInstruction
    {
        public string OperationCode => string.IsNullOrEmpty(Namespace) ? $"AS {TypeName}" : $"AS {Namespace}.{TypeName}";
        public string TypeName { get; }
        public string Namespace { get; }

        public As(string typeName, string @namespace)
        {
            TypeName = typeName;
            Namespace = @namespace;
        }

        public void Execute(RuntimeContext context)
        {
            var value = context.Stack.Pop();

            context.Stack.Push(context.Proxy.Casting.As(value, TypeName, Namespace));
        }
    }
}
