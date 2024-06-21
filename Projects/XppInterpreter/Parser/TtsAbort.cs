using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class TtsAbort : Statement
    {
        public TtsAbort(SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding) { }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitTtsAbort(this);
        }
    }
}
