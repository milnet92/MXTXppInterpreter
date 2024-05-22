using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class NoReturnFunctionCall : Statement
    {
        public FunctionCall FunctionCall{ get; set; }

        public NoReturnFunctionCall(FunctionCall funcCall, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            FunctionCall = funcCall;

            FunctionCall.DebuggeableBinding = null;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitNoReturnFunctionCall(this);
        }
    }
}
