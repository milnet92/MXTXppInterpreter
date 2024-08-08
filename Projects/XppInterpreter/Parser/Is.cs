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
    public class Is : Expression
    {
        public Expression Expression { get; }
        public string TypeName { get; }

        public Is(Expression expression, string typeName, SourceCodeBinding sourceCodeBinding)
            : base(Word.Is, sourceCodeBinding, null)
        {
            Expression = expression;
            TypeName = typeName;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitIs(this);
        }

        internal override System.Type Accept(ITypeInferExpressionVisitor inferer)
        {
            return inferer.VisitIs(this);
        }
    }
}
