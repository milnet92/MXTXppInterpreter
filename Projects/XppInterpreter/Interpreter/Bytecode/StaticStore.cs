namespace XppInterpreter.Interpreter.Bytecode
{
    /// <summary>
    /// Assigns a static field or property: <c>MyClass::field = value</c>
    /// </summary>
    class StaticStore : IInstruction
    {
        public string OperationCode => $"STORE_STATIC {ClassName}::{Name}";
        public string Name { get; }
        public string ClassName { get; }

        /// <summary>
        /// The .NET type of the member, or null for an X++ class
        /// </summary>
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
