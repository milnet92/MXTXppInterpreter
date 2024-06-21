using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Breakpoint : Statement
    {
        public Breakpoint(SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding) { }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitBreakpoint(this);
        }
    }
}
