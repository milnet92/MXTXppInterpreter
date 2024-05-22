using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Switch : Statement
    {
        public Expression Expression { get; }
        public IDictionary<Expression, List<Statement>> Cases { get; }
        public List<Statement> Default { get; }
        public Switch(
            Expression switchExpression,
            IDictionary<Expression, List<Statement>> cases,
            List<Statement> defaultStatement,
            SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableCodeBinding) : base(sourceCodeBinding, debuggeableCodeBinding)
        {
            Expression = switchExpression;
            Cases = cases ?? new Dictionary<Expression, List<Statement>>();
            Default = defaultStatement ?? new List<Statement>();
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitSwitch(this);
        }
    }
}
