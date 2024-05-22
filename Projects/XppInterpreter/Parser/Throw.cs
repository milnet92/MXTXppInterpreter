using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Throw : Statement
    {
        public Expression Exception { get; }

        public Throw(Expression exception, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            Exception = exception;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitThrow(this);
        }
    }
}
