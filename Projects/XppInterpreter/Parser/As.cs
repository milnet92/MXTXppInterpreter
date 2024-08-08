using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;
using XppInterpreter.Parser.Metadata;

namespace XppInterpreter.Parser
{
    public class As : Expression
    {
        public Expression Expression { get; }
        public string TypeName { get; }

        public As(Expression expression, string typeName, SourceCodeBinding sourceCodeBinding)
            : base(Word.As, sourceCodeBinding)
        {
            Expression = expression;
            TypeName = typeName;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitAs(this);
        }

        internal override System.Type Accept(ITypeInferExpressionVisitor inferer)
        {
            return inferer.VisitAs(this);
        }
    }
}
