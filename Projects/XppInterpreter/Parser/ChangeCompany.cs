using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class ChangeCompany : Statement
    {
        public Expression Expression { get; }
        public Block Block { get; }

        public ChangeCompany(Expression expression, Block block, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(sourceCodeBinding, debuggeableBinding)
        {
            Expression = expression;
            Block = block;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitChangeCompany(this);
        }
    }
}
