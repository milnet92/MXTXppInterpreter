using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Parser
{
    public class While : Loop
    {
        public Expression Expression { get; }

        public While(Expression expression, Block block, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(block, sourceCodeBinding, debuggeableBinding)
        {
            Expression = expression;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitWhile(this);
        }
    }
}
