using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Print : Statement
    {
        public List<Expression> Parameters { get; }

        public Print(List<Expression> parameters, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(sourceCodeBinding, debuggeableBinding)
        {
            Parameters = parameters;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitPrint(this);
        }
    }
}
