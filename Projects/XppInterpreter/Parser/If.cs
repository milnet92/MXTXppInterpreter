using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Parser
{
    public class If : Statement, IDebuggeable
    {
        public Expression Expression { get; }
        public Statement Statement { get; }
        public If Else { get; }

        public If(
            Expression expression,
            Statement statement,
            If @else,
            SourceCodeBinding sourceCodeBinding,
            SourceCodeBinding debuggeableBinding) : base(sourceCodeBinding, debuggeableBinding)
        {
            Expression = expression;
            Statement = statement;
            Else = @else;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitIf(this);
        }
    }
}
