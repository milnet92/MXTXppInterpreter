using XppInterpreter.Interpreter.Query;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Next : IInstruction
    {
        public string OperationCode => "NEXT";

        public void Execute(RuntimeContext context)
        {
            object record = context.Stack.Pop();

            if (context.PendingFetches.TryGetValue(record, out ISearchInstance search))
            {
                context.PendingFetches.Remove(record);
                search.Next();
            }
            else
            {
                context.Proxy.Data.Next(record);
            }
        }
    }
}
