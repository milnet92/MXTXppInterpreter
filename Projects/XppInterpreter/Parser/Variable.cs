using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class Variable : Expression
    {
        public string Name => (Token as Word).Lexeme;
        public Expression Caller { get; }
        public bool StaticCall { get; }

        public Variable(Word identifier, Expression caller, bool staticCall, SourceCodeBinding sourceCodeBinding) : base(identifier, sourceCodeBinding)
        {
            Caller = caller;
            StaticCall = staticCall;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitVariable(this);
        }
    }
}
