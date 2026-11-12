namespace XppInterpreter.Interpreter.Bytecode
{
    class Return : IInstruction
    {
        public string OperationCode => "RETURN";
        public bool HasValue { get; }

        public Return(bool hasValue)
        {
            HasValue = hasValue;
        }

        public void Execute(RuntimeContext context)
        {
            if (HasValue)
            {
                context.ReturnValue = context.Stack.Pop();
            }

            int handlers = context.ScopeHandler.ExceptionHandlerCount;

            if (handlers > 0)
            {
                context.BeginExit(PendingExit.Return(handlers));
            }
            else
            {
                context.Returned = true;
            }
        }
    }
}
