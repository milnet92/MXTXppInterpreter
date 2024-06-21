using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class Constructor : FunctionCall
    {
        public string ClassName => (Token as Word).Lexeme;

        public Constructor(Word identifier, List<Expression> parameters, Expression caller, bool staticCall, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) :
            base(identifier, parameters, caller, staticCall, false, sourceCodeBinding, debuggeableBinding)
        {
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitConstructor(this);
        }
    }
}
