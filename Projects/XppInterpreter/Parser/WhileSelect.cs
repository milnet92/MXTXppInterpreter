using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class WhileSelect : Statement
    {
        public Select Select { get; }
        public Block Block { get; }

        public WhileSelect(Select select, Block block, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(sourceCodeBinding, debuggeableBinding)
        {
            Select = select;
            Block = block;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitWhileSelect(this);
        }
    }
}
