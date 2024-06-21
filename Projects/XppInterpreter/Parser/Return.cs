using System.Diagnostics;
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

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitReturn(this);
        }
    }
}
