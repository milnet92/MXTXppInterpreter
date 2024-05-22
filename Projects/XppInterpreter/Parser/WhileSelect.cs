using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Parser.Data;

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

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitWhileSelect(this);
        }
    }
}
