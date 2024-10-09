namespace XppInterpreter.Interpreter.Bytecode
{
    class TtsCommit : IInstruction
    {
        public string OperationCode => "TTSCOMMIT";

        public void Execute(RuntimeContext context)
        {
            context.Proxy.Data.TtsCommit();

            if (context.ScopeHandler.AreExceptionsHandled)
            {
                context.ScopeHandler.CurrentExceptionHandler.DecreaseTransactionCounter();
            }
        }
    }
}
