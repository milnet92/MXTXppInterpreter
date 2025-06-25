namespace XppInterpreter.Interpreter.Bytecode
{
    class DisposeHandle : IInstruction
    {
        public string OperationCode => "DISPOSE HANDLE";

        public void Execute(RuntimeContext context)
        {
            context.Disposables.Pop().Dispose();
        }
    }
}
