using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Program : Statement
    {
        public List<Statement> Statements { get; }

        public Program(List<Statement> statements, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, null)
        {
            Statements = statements;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitProgram(this);
        }
    }
}
