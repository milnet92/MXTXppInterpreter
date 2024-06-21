using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Parser
{
    public class Assignment : Statement
    {
        public Variable Assignee { get; }
        public Expression Expression { get; }

        public Assignment(Variable assignee, Expression expression, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            Assignee = assignee;
            Expression = expression;

            if (Expression is IDebuggeable debuggeable)
            {
                debuggeable.DebuggeableBinding = null;
            }
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitAssignment(this);
        }
    }
}
