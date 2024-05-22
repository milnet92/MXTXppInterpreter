using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Program : Statement
    {
        public List<Statement> Statements { get; }

        public Program(List<Statement> statements, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, null)
        {
            Statements = statements;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitProgram(this);
        }
    }
}
