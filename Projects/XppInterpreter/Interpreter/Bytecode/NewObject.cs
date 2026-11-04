namespace XppInterpreter.Interpreter.Bytecode
{
    class NewObject : Call
    {
        public override string OperationCode => $"NEW_OBJ {Name} {NArgs}";

        /// <summary>
        /// The .NET type to create, or null for an X++ class
        /// </summary>
        public System.Type ClrType { get; set; }

        public NewObject(string className, int nArgs, bool alloc) : base(className, nArgs, alloc) { }

        public override object MakeCall(RuntimeContext context, object[] arguments)
        {
            if (ClrType != null)
            {
                return Core.Clr.ClrBinder.Construct(ClrType, arguments, ArgumentTypes);
            }

            return context.Proxy.Reflection.CreateInstance(Name, arguments);
        }
    }
}
