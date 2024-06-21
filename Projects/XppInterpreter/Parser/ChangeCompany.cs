using System.Diagnostics;
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

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitChangeCompany(this);
        }
    }
}
