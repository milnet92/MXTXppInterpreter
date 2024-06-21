namespace XppInterpreter.Interpreter.Bytecode
{
    class NewObject : Call
    {
        public override string OperationCode => $"NEW_OBJ {Name} {NArgs}";

        public NewObject(string className, int nArgs, bool alloc) : base(className, nArgs, alloc) { }

        public override object MakeCall(RuntimeContext context, object[] arguments)
        {
            return context.Proxy.Reflection.CreateInstance(Name, arguments);
        }
    }
}
