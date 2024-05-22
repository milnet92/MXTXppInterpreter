using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Jump : IInstruction
    {
        public virtual string OperationCode => $"JUMP {Offset}";
        public int Offset { get; protected set; }
        public Jump(int offset)
        {
            Offset = offset;
        }

        public virtual void Execute(RuntimeContext context)
        {
            context.moveCounter(Offset);
        }
    }
}
