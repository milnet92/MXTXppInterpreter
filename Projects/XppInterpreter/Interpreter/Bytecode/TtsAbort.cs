namespace XppInterpreter.Interpreter.Bytecode
{
    class TtsAbort : IInstruction
    {
        public string OperationCode => "TTSABORT";

        public void Execute(RuntimeContext context)
        {
            context.Proxy.Data.TtsAbort();
        }
    }
}
