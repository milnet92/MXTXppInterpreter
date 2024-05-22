using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Parser
{
    public class For : Loop
    {
        public Statement Initialisation { get; }
        public Statement LoopStatement { get; }
        public Expression Condition { get; }

        public For(Statement initialisation, Expression condition, Statement loopStatement, Block block, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(block, sourceCodeBinding, debuggeableBinding)
        {
            Initialisation = initialisation;
            Condition = condition;
            LoopStatement = loopStatement;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitFor(this);
        }
    }
}
