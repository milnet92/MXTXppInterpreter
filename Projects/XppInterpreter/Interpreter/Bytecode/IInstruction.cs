namespace XppInterpreter.Interpreter.Bytecode
{
    public interface IInstruction
    {
        string OperationCode { get; }
        void Execute(RuntimeContext context);
    }
}
