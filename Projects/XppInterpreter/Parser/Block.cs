using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Block : Statement
    {
        public List<Statement> Statements { get; }

        public Block(List<Statement> statements, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(sourceCodeBinding, debuggeableBinding)
        {
            Statements = statements;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitBlock(this);
        }
    }
}
