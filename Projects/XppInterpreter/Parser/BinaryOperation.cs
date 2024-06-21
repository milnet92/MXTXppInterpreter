using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class BinaryOperation : Expression
    {
        public Expression LeftOperand { get; }
        public Expression RightOperand { get; }

        public BinaryOperation(Expression left, Expression right, Token @operator, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding = null) : base(@operator, sourceCodeBinding, debuggeableBinding)
        {
            LeftOperand = left;
            RightOperand = right;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitBinaryOperation(this);
        }
    }
}
