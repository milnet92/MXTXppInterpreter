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

            if (!context.Proxy.Casting.ToBoolean(value))
            {
                if (ReturnToStack)
                    context.Stack.Push(false);

                base.Execute(context);
            }
        }
    }
}
