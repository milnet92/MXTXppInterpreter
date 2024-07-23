using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;
using XppInterpreter.Parser.Completer;

namespace XppInterpreter.Parser
{
    public class Ternary : Expression
    {
        public Expression Condition { get; }
        public Expression Left { get; }
        public Expression Right { get; }

        public Ternary(Token token, Expression check, Expression left, Expression right, SourceCodeBinding sourceCodeBinding) : base(token, sourceCodeBinding)
        {
            Condition = check;
            Left = left;
            Right = right;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitTernary(this);
        }
        internal override System.Type Accept(ITypeInferExpressionVisitor inferer)
        {
            return inferer.VisitTernary(this);
        }
    }
}
