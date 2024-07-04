using XppInterpreter.Interpreter.Query;

namespace XppInterpreter.Interpreter.Bytecode
{
    class InserQuery : IInstruction
    {
        public string OperationCode => "INSERT_QUERY";
        public Parser.InsertRecordset InsertRecordset { get; }

        public InserQuery(Parser.InsertRecordset insertRecordset)
        {
            InsertRecordset = insertRecordset;
        }

        public void Execute(RuntimeContext context)
        {
            context.Proxy.QueryGeneration.NewQueryGenerator().ExecuteInsertRecordset(InsertRecordset, context);
        }
    }
}
