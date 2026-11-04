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

        public System.Type ClrType { get; set; }

        public bool IsMapCall { get; set; }

        public As(string typeName)
        {
            TypeName = typeName;
        }

        public void Execute(RuntimeContext context)
        {
            var value = context.Stack.Pop();

            if (IsMapCall)
            {
                context.Stack.Push(context.Proxy.Casting.Cast(value, context.Proxy.Casting.GetSystemTypeFromTypeName(TypeName)));
                return;
            }

            context.Stack.Push(ClrType != null ? (ClrType.IsInstanceOfType(value) ? value : null) : context.Proxy.Casting.As(value, TypeName));
        }
    }
}
