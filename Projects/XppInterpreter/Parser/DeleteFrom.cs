using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Parser.Data;

namespace XppInterpreter.Parser
{
    public class DeleteFrom : Statement
    {
        public string TableVariableName { get; }
        public Where Where { get; set; }
        public Join Join { get; set; }

        public DeleteFrom(string tableVariableName, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            TableVariableName = tableVariableName;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitDeleteFrom(this);
        }
    }
}
