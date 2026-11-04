namespace XppInterpreter.Interpreter.Bytecode
{
    class NewObject : Call
    {
        public override string OperationCode => $"NEW_OBJ {Name} {NArgs}";

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
