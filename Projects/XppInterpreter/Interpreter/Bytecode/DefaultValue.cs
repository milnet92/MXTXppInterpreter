namespace XppInterpreter.Interpreter.Bytecode
{
    class DefaultValue : IInstruction
    {
        public virtual string OperationCode => "PUSH_DEFAULT";
        public string TypeName { get; }
        public string Namespace { get; }

        public DefaultValue(string typeName, string @namespace)
        {
            TypeName = typeName;
            Namespace = @namespace;
        }

        public virtual void Execute(RuntimeContext context)
        {
            context.Stack.Push(context.Proxy.Casting.GetDefaultValueForType(TypeName, Namespace));
        }
    }
}
