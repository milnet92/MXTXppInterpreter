using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class TtsCommit : Statement
    {
        public TtsCommit(SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding) { }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitTtsCommit(this);
        }
    }
}
