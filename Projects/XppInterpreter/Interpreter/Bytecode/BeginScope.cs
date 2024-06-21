namespace XppInterpreter.Interpreter.Bytecode
{
    class BeginScope : IInstruction
    {
        public string OperationCode => "BEGIN_SCOPE";

        public void Execute(RuntimeContext context)
        {
            context.ScopeHandler.BeginScope();
        }
    }
}
