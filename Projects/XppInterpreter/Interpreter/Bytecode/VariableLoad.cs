namespace XppInterpreter.Interpreter.Bytecode
{
    class VariableLoad : Load
    {
        public VariableLoad(string name, bool isArray) : base(name, isArray) { }

        public override object MakeLoad(RuntimeContext context)
        {
            return context.ScopeHandler.GetVar(Name);
        }

        public override object MakeLoadFromArray(RuntimeContext context, int index)
        {
            var array = MakeLoad(context);

            return context.Proxy.Casting.GetArrayIndexValue(array, index);
        }
    }
}
