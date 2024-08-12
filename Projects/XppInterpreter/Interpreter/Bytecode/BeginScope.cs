namespace XppInterpreter.Interpreter.Bytecode
{
    class BeginScope : IInstruction
    {
        public virtual string OperationCode => "BEGIN_SCOPE";

        public virtual void Execute(RuntimeContext context)
        {
            context.ScopeHandler.BeginScope();
        }
    }
}
