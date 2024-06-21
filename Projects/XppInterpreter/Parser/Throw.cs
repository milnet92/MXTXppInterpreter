using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Throw : Statement
    {
        public Expression Exception { get; }

        public Throw(Expression exception, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            Exception = exception;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitThrow(this);
        }
    }
}
