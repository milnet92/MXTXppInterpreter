using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class TtsAbort : Statement
    {
        public TtsAbort(SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding) { }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitTtsAbort(this);
        }
    }
}
