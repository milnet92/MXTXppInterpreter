namespace XppInterpreter.Interpreter.Bytecode
{
    class EndScope : IInstruction
    {
        public virtual string OperationCode => "END_SCOPE";

        public virtual void Execute(RuntimeContext context)
        {
            context.ScopeHandler.EndScope();
        }
    }
}
