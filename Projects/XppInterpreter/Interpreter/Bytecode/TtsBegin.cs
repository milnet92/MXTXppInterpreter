namespace XppInterpreter.Interpreter.Bytecode
{
    class TtsBegin : IInstruction
    {
        public string OperationCode => "TTSBEGIN";

        public void Execute(RuntimeContext context)
        {
            context.Proxy.Data.TtsBegin();

            if (context.ScopeHandler.AreExceptionsHandled)
            {
                context.ScopeHandler.CurrentExceptionHandler.IncreaseTransactionCounter();
            }
        }
    }
}
