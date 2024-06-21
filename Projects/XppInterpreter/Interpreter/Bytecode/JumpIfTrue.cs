namespace XppInterpreter.Interpreter.Bytecode
{
    class JumpIfTrue : Jump
    {
        public override string OperationCode => $"JUMP_IF_TRUE {Offset}";

        public JumpIfTrue(int offset) : base(offset) { }

        public override void Execute(RuntimeContext context)
        {
            var value = context.Stack.Pop();

            if (context.Proxy.Casting.ToBoolean(value))
                base.Execute(context);

            context.Stack.Push(value);
        }
    }
}
