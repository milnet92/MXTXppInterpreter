using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Parser
{
    public class ContainerAssignment : Statement
    {
        public List<Variable> Assignees { get;}
        public Expression Expression { get; }

        public ContainerAssignment(List<Variable> assignees, Expression expression, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            Assignees = assignees;
            Expression = expression;

            if (Expression is IDebuggeable debuggeable)
            {
                debuggeable.DebuggeableBinding = null;
            }
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitContainerAssignment(this);
        }
    }
}
