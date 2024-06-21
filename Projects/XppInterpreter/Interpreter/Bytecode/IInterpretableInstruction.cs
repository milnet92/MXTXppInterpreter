namespace XppInterpreter.Interpreter.Bytecode
{
    public interface IInterpretableInstruction
    {
        InterpreterResult LastResult { get; }
    }
}
