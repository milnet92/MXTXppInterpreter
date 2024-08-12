using System;
using System.Linq;
using XppInterpreter.Lexer;

namespace XppInterpreter.Interpreter.Bytecode
{
    class LoopControl : Jump
    {
        public override string OperationCode => TokenType == TType.Break ? $"BREAK {Offset}" : $"CONTINUE {Offset}";
        public TType TokenType { get; }
        public bool IsDirty { get; private set; }

        public LoopControl(int offset, TType tokenType, bool isDirty) : base(offset)
        {
            TokenType = tokenType;
            IsDirty = isDirty;
        }

        public override void Execute(RuntimeContext context)
        {
            int start = Offset < 0 ? Math.Abs(context.Counter + Offset) : context.Counter;

            // Close un-balanced scopes
            context.EndScopeRange(start, Math.Abs(Offset));

            base.Execute(context);
        }

        public void SetFinalOffset(int offset)
        {
            Offset = offset;
            IsDirty = false;
        }
    }
}
