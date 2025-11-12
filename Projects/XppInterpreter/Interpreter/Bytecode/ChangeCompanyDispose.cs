namespace XppInterpreter.Interpreter.Bytecode
{
    class ChangeCompanyDispose : IInstruction
    {
        public string OperationCode => "DISPOSE_COMPANY";

        public void Execute(RuntimeContext context)
        {
            context.ChangeCompanyHandlers.Pop().Dispose();
        }
    }
}
