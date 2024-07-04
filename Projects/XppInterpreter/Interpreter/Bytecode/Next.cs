using XppInterpreter.Interpreter.Query;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Next : IInstruction
    {
        public string OperationCode => "NEXT";

        public void Execute(RuntimeContext context)
        {
            context.Proxy.Data.Next(context.Stack.Pop());
        }
    }
}
