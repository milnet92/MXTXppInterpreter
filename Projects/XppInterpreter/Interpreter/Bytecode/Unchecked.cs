using System;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Unchecked : IInstruction
    {
        public string OperationCode => "UNCHECKED";

        public void Execute(RuntimeContext context)
        {
            int flag = (int)context.Proxy.Casting.Cast(context.Stack.Pop(), typeof(int));
            IDisposable uncheckedDisposable = context.Proxy.Data.CreateUncheckedHandler(flag, "XppInterpreter", "Interpret");
            context.Disposables.Push(uncheckedDisposable);
        }
    }
}