namespace XppInterpreter.Interpreter.Bytecode
{
    class StaticLoad : Load
    {
        public string CallerName { get; }

        /// <summary>
        /// The .NET type of the member, or null for an X++ class or enumeration
        /// </summary>
        public System.Type ClrType { get; set; }

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

            if (context.Proxy.Reflection.IsEnum(CallerName))
            {
                return context.Proxy.Reflection.GetEnumValue(CallerName, Name);
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
