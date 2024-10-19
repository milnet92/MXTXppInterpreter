using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class ArrayAccess : Variable
    {
        public Expression Index { get; }

        public ArrayAccess(Word identifier, Expression caller, Expression index, bool staticCall, SourceCodeBinding sourceCodeBinding) : base(identifier, caller, staticCall, "", sourceCodeBinding)
        {
            Index = index;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            base.Accept(interpreter);
        }
    }
}
