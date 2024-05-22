using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Parser.Data;

namespace XppInterpreter.Parser
{
    public class Select : Statement
    {
        public Query Query { get; }

        public Select(Query query, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            Query = query;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitSelect(this);
        }
    }
}
