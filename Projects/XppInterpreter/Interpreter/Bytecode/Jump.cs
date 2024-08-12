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
            context.MoveCounter(Offset);
        }
    }
}
