namespace XppInterpreter.Interpreter.Bytecode
{
    class TtsAbort : IInstruction
    {
        public string OperationCode => "TTSABORT";

        public void Execute(RuntimeContext context)
        {
            context.Proxy.Data.TtsAbort();

            if (context.ScopeHandler.AreExceptionsHandled)
            {
                context.ScopeHandler.CurrentExceptionHandler.DecreaseTransactionCounter();
            }
        }
    }
}
