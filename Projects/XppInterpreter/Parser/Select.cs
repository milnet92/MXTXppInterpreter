using System.Diagnostics;
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

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitSelect(this);
        }
    }
}
