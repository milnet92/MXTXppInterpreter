using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Do : Loop
    {
        public Expression Expression { get; }

        public Do(Expression expression, Block block, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(block, sourceCodeBinding, debuggeableBinding)
        {
            Expression = expression;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitDo(this);
        }
    }
}
