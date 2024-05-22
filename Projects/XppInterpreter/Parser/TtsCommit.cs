using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class TtsCommit : Statement
    {
        public TtsCommit(SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding) { }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitTtsCommit(this);
        }
    }
}
