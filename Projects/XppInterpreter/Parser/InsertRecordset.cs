using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;

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

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitInsertRecordset(this);
        }
    }
}
