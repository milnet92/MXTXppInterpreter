using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Parser.Data;

namespace XppInterpreter.Parser
{
    public class InsertRecordset : Statement
    {
        public string TableVariableName { get; }
        public List<string> FieldList { get; }
        public Select Select { get; }

        public InsertRecordset(string tableVariableName, List<string> insertFieldList, Select select, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            TableVariableName = tableVariableName;
            FieldList = insertFieldList;
            Select = select;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitInsertRecordset(this);
        }
    }
}
