using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            new QueryGenerator(context).ExecuteInsertRecordset(InsertRecordset);
        }
    }
}
