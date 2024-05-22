using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class JumpIfFalse : Jump
    {
        public override string OperationCode => $"JUMP_IF_FALSE {Offset}";

        public JumpIfFalse(int offset) : base(offset) { }

        public override void Execute(RuntimeContext context)
        {
            var value = context.Stack.Pop();

            if (!context.Proxy.Casting.ToBoolean(value))
                base.Execute(context);

            //context.Stack.Push(value);
        }
    }
}
