using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class While : Loop
    {
        public Expression Expression { get; }

        public While(Expression expression, Block block, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(block, sourceCodeBinding, debuggeableBinding)
        {
            Expression = expression;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitWhile(this);
        }
    }
}
