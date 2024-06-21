using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class TtsBegin : Statement
    {
        public TtsBegin(SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding) { }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitTtsBegin(this);
        }
    }
}
