﻿using XppInterpreter.Interpreter.Query;

namespace XppInterpreter.Interpreter.Bytecode
{
    class UpdateQuery : IInstruction
    {
        public string OperationCode => "UPDATE_QUERY";
        public Parser.UpdateRecordset UpdateRecordset { get; }

        public UpdateQuery(Parser.UpdateRecordset updateRecordset)
        {
            UpdateRecordset = updateRecordset;
        }

        public void Execute(RuntimeContext context)
        {
            context.Proxy.QueryGeneration.NewQueryGenerator().ExecuteUpdateRecordset(UpdateRecordset, context);
        }
    }
}
