namespace XppInterpreter.Interpreter.Bytecode
{
    class InstanceFunctionCall : Call
    {
        public InstanceFunctionCall(string funcName, int nArgs, bool alloc) : base(funcName, nArgs, alloc) { }

        public override object MakeCall(RuntimeContext context, object[] arguments)
        {
            // The caller is evaluated before the arguments, so it is below them
            var instance = context.Stack.Pop();

            if (instance is null || !context.Proxy.Reflection.IsXppType(instance.GetType()))
            {
                return Core.Clr.ClrBinder.InvokeInstance(instance, Name, arguments, ArgumentTypes);
            }

            arguments = Core.ReflectionHelper.PackVariableArguments(instance.GetType(), Name, arguments);

            return context.Proxy.Reflection.CallInstanceFunction(instance, Name, arguments);
        }
    }
}
