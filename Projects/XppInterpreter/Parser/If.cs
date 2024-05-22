using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Parser
{
    public class If : Statement, IDebuggeable
    {
        public Expression Expression { get; }
        public Statement Statement { get; }
        public If Else { get; }

        public If(
            Expression expression, 
            Statement statement, 
            If @else, 
            SourceCodeBinding sourceCodeBinding,
            SourceCodeBinding debuggeableBinding) : base(sourceCodeBinding, debuggeableBinding)
        {
            Expression = expression;
            Statement = statement;
            Else = @else;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitIf(this);
        }
    }
}
