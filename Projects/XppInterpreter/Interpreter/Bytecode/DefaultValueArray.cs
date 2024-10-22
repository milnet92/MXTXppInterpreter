namespace XppInterpreter.Interpreter.Bytecode
{
    class DefaultValueArray : DefaultValue
    {
        public override string OperationCode => $"PUSH_DEFAULT_A";
        public bool FixedSize { get; }

        public DefaultValueArray(string typeName, string @namespace, bool fixedSize) : base(typeName, @namespace)
        {
            FixedSize = fixedSize;
        }

        public override void Execute(RuntimeContext context)
        {
            object defaultValue;

            if (FixedSize)
            {
                int size = (int)context.Stack.Pop();
                defaultValue = context.Proxy.Casting.CreateFixedArray(TypeName, Namespace, size);
            }
            else
            {
                defaultValue = context.Proxy.Casting.CreateDynamicArray(TypeName, Namespace);
            }

            context.Stack.Push(defaultValue);
        }
    }
}
