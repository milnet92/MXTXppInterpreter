namespace XppInterpreter.Interpreter.Bytecode
{
    class JumpIfTrue : Jump
    {
        public override string OperationCode => $"JUMP_IF_TRUE {Offset}";
        public bool ReturnToStack { get; set; }
        public JumpIfTrue(int offset, bool returnToStack = true) : base(offset)
        {
            ReturnToStack = returnToStack;
        }

        public override void Execute(RuntimeContext context)
        {
            var value = context.Stack.Pop();

            if (Core.Clr.ClrValues.ToBoolean(value, context.Proxy))
            {
                if (ReturnToStack)
                    context.Stack.Push(true);

                base.Execute(context);
            }
        }
    }
}
