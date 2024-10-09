namespace XppInterpreter.Interpreter.Bytecode
{
    class StaticFunctionCall : Call
    {
        public string ClassName { get; }
        public StaticFunctionCall(string name, int nArgs, bool alloc, string className = null) : base(name, nArgs, alloc)
        {
            ClassName = className;
        }

        public override object MakeCall(RuntimeContext context, object[] arguments)
        {
            if (string.IsNullOrEmpty(ClassName))
            {
                return context.Proxy.Reflection.CallGlobalOrPredefinedFunction(context, Name, arguments);
            }
            else
            {
                return context.Proxy.Reflection.CallStaticFunction(ClassName, Name, arguments);
            }
        }
    }
}
