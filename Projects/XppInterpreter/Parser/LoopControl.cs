using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class LoopControl : Statement
    {
        public Token Token { get; set; }

        public LoopControl(Token token, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            Token = token;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitLoopControl(this);
        }
    }
}
