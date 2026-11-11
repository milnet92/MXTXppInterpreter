namespace XppInterpreter.Interpreter.Bytecode
{
    class StaticLoad : Load
    {
        public string CallerName { get; }

        public System.Type ClrType { get; set; }

        // An enum literal is the same value every time it runs: a condition inside a loop asked for it on each iteration
        private object _enumValue;

        public StaticLoad(string name, string callerName, bool isArray) : base(name, isArray)
        {
            CallerName = callerName;
        }

        public override object MakeLoad(RuntimeContext context)
        {
            if (ClrType != null)
            {
                return Core.Clr.ClrBinder.GetStaticMember(ClrType, Name);
            }

            if (_enumValue != null)
            {
                return _enumValue;
            }

            if (context.Proxy.IsEnum(CallerName))
            {
                _enumValue = context.Proxy.Reflection.GetEnumValue(CallerName, Name);

                return _enumValue;
            }
            else
            {
                return context.Proxy.Reflection.GetStaticProperty(CallerName, Name);
            }
        }

        public override object MakeLoadFromArray(RuntimeContext context, int index)
        {
            var array = MakeLoad(context);

            return context.Proxy.Casting.GetArrayIndexValue(array, index);
        }
    }
}
