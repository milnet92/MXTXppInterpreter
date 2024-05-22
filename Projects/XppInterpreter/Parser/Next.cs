using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Next : Statement
    {
        public string TableVariableName { get; }

        public Next(string tableVariableName, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            TableVariableName = tableVariableName;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitNext(this);
        }
    }
}
