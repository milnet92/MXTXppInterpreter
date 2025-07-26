using XppInterpreter.Core;

namespace XppInterpreter.Interpreter.Bytecode
{
    class InstanceLoad : Load
    {
        public InstanceLoad(string name, bool isArray) : base(name, isArray) { }

        public override object MakeLoad(RuntimeContext context)
        {
            object caller = context.Stack.Pop();

            // Check if it's a delegate
            var field = ReflectionHelper.GetField(caller.GetType(), Name);

            if (field != null && ReflectionHelper.IsDelegateType(field.FieldType))
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
