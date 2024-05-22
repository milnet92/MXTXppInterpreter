using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class TtsBegin : Statement
    {
        public TtsBegin(SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding) { }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitTtsBegin(this);
        }
    }
}
