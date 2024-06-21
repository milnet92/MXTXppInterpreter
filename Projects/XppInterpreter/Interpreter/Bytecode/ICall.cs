namespace XppInterpreter.Interpreter.Bytecode
{
    public interface ICall
    {
        string Name { get; }
        int NArgs { get; }
        bool Alloc { get; }
        bool ProcessParameters { get; }
    }
}
