using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class Ternary : Expression
    {
        public Expression Condition { get; set; }
        public Expression Left { get; set; }
        public Expression Right { get; set; }

        public Ternary(Token token, Expression check, Expression left, Expression right, SourceCodeBinding sourceCodeBinding) : base(token, sourceCodeBinding)
        {
            Condition = check;
            Left = left;
            Right = right;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitTernary(this);
        }
    }
}
