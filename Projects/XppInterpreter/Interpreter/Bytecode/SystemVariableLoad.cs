namespace XppInterpreter.Interpreter.Bytecode
{
    class SystemVariableLoad : IInstruction
    {
        public string OperationCode => $"LOAD_SYSTEM {Name}";
        public string Name { get; }

        public SystemVariableLoad(string name)
        {
            Name = name;
        }

        public void Execute(RuntimeContext context)
        {
            context.Stack.Push(context.Proxy.Reflection.GetSystemVariable(Name));
        }
    }
}
