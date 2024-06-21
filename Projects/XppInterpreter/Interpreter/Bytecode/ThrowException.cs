namespace XppInterpreter.Interpreter.Bytecode
{
    class ThrowException : IInstruction
    {
        public virtual string OperationCode => "THROW";

        public virtual void Execute(RuntimeContext context)
        {
            context.Proxy.Exceptions.Throw(context.Stack.Pop());
        }
    }
}
