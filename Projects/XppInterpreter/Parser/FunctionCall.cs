using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class FunctionCall : Variable, IDebuggeable
    {
        public List<Expression> Parameters { get; }
        public bool Intrinsical { get; }

        public FunctionCall(
            Word identifier,
            List<Expression> parameters, 
            Expression caller, 
            bool staticCall, 
            bool intrinsical,
            SourceCodeBinding sourceCodeBinding,
            SourceCodeBinding debuggeableBinding) : base(identifier, caller, staticCall, sourceCodeBinding)
        {
            Parameters = parameters;
            Intrinsical = intrinsical;
            DebuggeableBinding = debuggeableBinding;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitFunctionCall(this);
        }
    }
}
