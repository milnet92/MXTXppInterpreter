namespace XppInterpreter.Interpreter.Bytecode
{
    class NewObject : Call
    {
        public override string OperationCode => $"NEW_OBJ {Name} {NArgs}";

        public NewObject(string className, int nArgs, string nameSpace, bool alloc) : base(className, nArgs, alloc, nameSpace) { }

        public override object MakeCall(RuntimeContext context, object[] arguments)
        {
            return context.Proxy.Reflection.CreateInstance(Namespace, Name, arguments);
        }
    }
}
