using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class For : Loop
    {
        public Statement Initialisation { get; }
        public Statement LoopStatement { get; }
        public Expression Condition { get; }

        public For(Statement initialisation, Expression condition, Statement loopStatement, Block block, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(block, sourceCodeBinding, debuggeableBinding)
        {
            Initialisation = initialisation;
            Condition = condition;
            LoopStatement = loopStatement;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitFor(this);
        }
    }
}
