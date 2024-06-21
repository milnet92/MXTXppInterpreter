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
            // Closed un-balanced scopes
            int start = Offset < 0 ? Math.Abs(context.Counter + Offset) : context.Counter;

            var rangeToCheck = context.ByteCode.Instructions.GetRange(start, Math.Abs(Offset));

            int beginScopeCount = rangeToCheck.Count(instruction => instruction is BeginScope);
            int endScopeCount = rangeToCheck.Count(instruction => instruction is EndScope);

            for (int i = 0; i < Math.Abs(endScopeCount - beginScopeCount); i++)
            {
                context.ScopeHandler.EndScope();
            }

            base.Execute(context);
        }

        public void SetFinalOffset(int offset)
        {
            Offset = offset;
            IsDirty = false;
        }
    }
}
