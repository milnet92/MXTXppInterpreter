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

        /// <summary>
        /// The try statements between this instruction and its loop
        /// </summary>
        public int HandlersToLeave { get; }

        public LoopControl(int offset, TType tokenType, bool isDirty, int handlersToLeave = 0) : base(offset)
        {
            TokenType = tokenType;
            IsDirty = isDirty;
            HandlersToLeave = handlersToLeave;
        }

        public override void Execute(RuntimeContext context)
        {
            if (HandlersToLeave > 0)
            {
                context.BeginExit(PendingExit.Jump(context.Counter + Offset, HandlersToLeave));
                return;
            }

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
