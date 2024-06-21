namespace XppInterpreter.Interpreter.Bytecode
{
    class DefaultValue : IInstruction
    {
        public virtual string OperationCode => "PUSH_DEFAULT";
        public string TypeName { get; }

        public DefaultValue(string typeName)
        {
            TypeName = typeName;
        }

        public virtual void Execute(RuntimeContext context)
        {
            context.Stack.Push(context.Proxy.Casting.GetDefaultValueForType(TypeName));
        }
    }
}
