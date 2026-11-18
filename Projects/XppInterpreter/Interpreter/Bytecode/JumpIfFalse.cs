namespace XppInterpreter.Interpreter.Bytecode
{
    class JumpIfFalse : Jump
    {
        public override string OperationCode => $"JUMP_IF_FALSE {Offset}";
        public bool ReturnToStack { get; set; }

        public JumpIfFalse(int offset, bool returnToStack = true) : base(offset)
        {
            ReturnToStack = returnToStack;
        }

        public override void Execute(RuntimeContext context)
        {
            var value = context.Stack.Pop();

            if (!Core.Clr.ClrValues.ToBoolean(value, context.Proxy))
            {
                if (ReturnToStack)
                    context.Stack.Push(false);

                base.Execute(context);
            }
        }
    }
}
