using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Block : Statement
    {
        public List<Statement> Statements { get; }

        public Block(List<Statement> statements, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(sourceCodeBinding, debuggeableBinding)
        {
            Statements = statements;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitBlock(this);
        }
    }
}
