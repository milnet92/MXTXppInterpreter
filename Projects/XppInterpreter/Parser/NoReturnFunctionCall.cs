using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class NoReturnFunctionCall : Statement
    {
        public FunctionCall FunctionCall { get; }

        public NoReturnFunctionCall(FunctionCall funcCall, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            FunctionCall = funcCall;

            // Child function is not debuggeable
            FunctionCall.DebuggeableBinding = null;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitNoReturnFunctionCall(this);
        }
    }
}
