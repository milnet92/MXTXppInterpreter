using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Retry : Statement
    {
        public Retry(SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding) { }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitRetry(this);
        }
    }
}
