using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Parser.Completer;

namespace XppInterpreter.Parser
{
    public class ContainerInitialisation : Expression
    {
        public List<Expression> Elements { get; }

        public ContainerInitialisation(List<Expression> elements, SourceCodeBinding sourceCodeBinding) : base(new Lexer.Token(Lexer.TType.Container), sourceCodeBinding)
        {
            Elements = elements ?? new List<Expression>();
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitContainerInitialisation(this);
        }

        internal override System.Type Accept(ITypeInferExpressionVisitor inferer)
        {
            return inferer.VisitContainerInitialisation(this);
        }
    }
}
