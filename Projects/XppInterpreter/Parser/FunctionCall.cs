using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Lexer;
using XppInterpreter.Parser.Completer;

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

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitFunctionCall(this);
        }
        internal override System.Type Accept(ITypeInferExpressionVisitor inferer)
        {
            return inferer.VisitFunctionCall(this);
        }
    }
}
