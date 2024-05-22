using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class UnaryOperation : Expression
    {
        public Expression Expression { get; }

        public UnaryOperation(Token token, Expression expression, SourceCodeBinding sourceCodeBinding) : base(token, sourceCodeBinding)
        {
            Expression = expression;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitUnaryOperation(this);
        }
    }
}
