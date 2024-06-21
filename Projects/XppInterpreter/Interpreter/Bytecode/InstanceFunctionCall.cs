namespace XppInterpreter.Interpreter.Bytecode
{
    class InstanceFunctionCall : Call
    {
        public InstanceFunctionCall(string funcName, int nArgs, bool alloc) : base(funcName, nArgs, alloc, false) { }

        public override object MakeCall(RuntimeContext context, object[] arguments)
        {
            var instance = context.Stack.Pop();
            arguments = GetParametersFromStack(context.Stack);
            return context.Proxy.Reflection.CallInstanceFunction(instance, Name, arguments);
        }
    }
}
