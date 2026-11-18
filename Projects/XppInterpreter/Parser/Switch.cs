using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Switch : Statement
    {
        public Expression Expression { get; }
        public IDictionary<Expression, List<Statement>> Cases { get; }
        public List<Statement> Default { get; }

        public IDictionary<Expression, List<Expression>> ExtraCaseValues { get; }
        public Switch(
            Expression switchExpression,
            IDictionary<Expression, List<Statement>> cases,
            List<Statement> defaultStatement,
            SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableCodeBinding,
            IDictionary<Expression, List<Expression>> extraCaseValues = null) : base(sourceCodeBinding, debuggeableCodeBinding)
        {
            Expression = switchExpression;
            Cases = cases ?? new Dictionary<Expression, List<Statement>>();
            Default = defaultStatement ?? new List<Statement>();
            ExtraCaseValues = extraCaseValues ?? new Dictionary<Expression, List<Expression>>();
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitSwitch(this);
        }
    }
}
