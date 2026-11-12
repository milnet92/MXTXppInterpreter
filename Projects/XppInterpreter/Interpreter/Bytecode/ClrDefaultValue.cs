using System;

namespace XppInterpreter.Interpreter.Bytecode
{
    class ClrDefaultValue : IInstruction
    {
        public string OperationCode => "PUSH_CLR_DEFAULT";
        public Type ClrType { get; }

        public ClrDefaultValue(Type clrType)
        {
            ClrType = clrType;
        }

        public void Execute(RuntimeContext context)
        {
            context.Stack.Push(ClrType.IsValueType && Nullable.GetUnderlyingType(ClrType) is null ? Activator.CreateInstance(ClrType) : null);
        }
    }
}
