using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Query
{
    public interface IQueryGenerator
    {
        void ExecuteInsertRecordset(Parser.InsertRecordset insertRecordset, RuntimeContext runtimeContext);
        void ExecuteDeleteFrom(Parser.DeleteFrom deleteFrom, RuntimeContext runtimeContext);
        void ExecuteUpdateRecordset(Parser.UpdateRecordset updateRecordset, RuntimeContext runtimeContext);
        ISearchInstance NewSearchInstance(Parser.Data.Query query, RuntimeContext runtimeContext, bool isFromExpression, object _buffer);
    }
}
