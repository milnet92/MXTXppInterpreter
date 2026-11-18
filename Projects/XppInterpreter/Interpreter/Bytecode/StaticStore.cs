namespace XppInterpreter.Interpreter.Bytecode
{
    class StaticStore : IInstruction
    {
        public string OperationCode => $"STORE_STATIC {ClassName}::{Name}";
        public string Name { get; }
        public string ClassName { get; }

        public System.Type ClrType { get; set; }

        public StaticStore(string name, string className)
        {
            Name = name;
            ClassName = className;
        }

        public void Execute(RuntimeContext context)
        {
            var value = context.Stack.Pop();

            if (ClrType != null)
            {
                Core.Clr.ClrBinder.SetStaticMember(ClrType, Name, value);
            }
            else
            {
                context.Proxy.Reflection.SetStaticProperty(ClassName, Name, value);
            }
        }
    }
}
