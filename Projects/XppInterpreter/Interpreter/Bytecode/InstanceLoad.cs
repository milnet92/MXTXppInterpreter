using XppInterpreter.Core;

namespace XppInterpreter.Interpreter.Bytecode
{
    class InstanceLoad : Load
    {
        public InstanceLoad(string name, bool isArray) : base(name, isArray) { }

        public override object MakeLoad(RuntimeContext context)
        {
            object caller = context.Stack.Pop();

            if (caller is null || !context.Proxy.IsXppType(caller.GetType()))
            {
                return Core.Clr.ClrBinder.GetMember(caller, Name);
            }

            if (ReflectionHelper.IsDelegateField(caller.GetType(), Name))
            {
                return new EventSubscriptionHandle(caller, Name);
            }

            return context.Proxy.Reflection.GetInstanceProperty(caller, Name);
        }

        public override object MakeLoadFromArray(RuntimeContext context, int index)
        {
            var array = MakeLoad(context);

            return context.Proxy.Casting.GetArrayIndexValue(array, index);
        }
    }
}
