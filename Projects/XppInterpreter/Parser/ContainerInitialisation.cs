using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class ContainerInitialisation : Expression
    {
        public List<Expression> Elements { get; }

        public ContainerInitialisation(List<Expression> elements, SourceCodeBinding sourceCodeBinding) : base(new Lexer.Token(Lexer.TType.Container), sourceCodeBinding)
        {
            Elements = elements ?? new List<Expression>();
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitContainerInitialisation(this);
        }
    }
}
