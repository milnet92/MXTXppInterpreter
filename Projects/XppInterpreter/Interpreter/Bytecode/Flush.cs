using XppInterpreter.Interpreter.Query;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Flush : IInstruction
    {
        public string OperationCode => "FLUSH";

        public string TableName { get; }

        public Flush(string tableName)
        {
            TableName = tableName;
        }

        public void Execute(RuntimeContext context)
        {
            context.Proxy.Data.Flush(TableName);
        }
    }
}
