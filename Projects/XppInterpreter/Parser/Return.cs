using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Return : Statement
    {
        public Expression Expression { get; }

        public Return(Expression returnExpression, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            Expression = returnExpression;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitReturn(this);
        }
    }
}
