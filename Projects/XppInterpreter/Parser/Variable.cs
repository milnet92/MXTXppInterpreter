using System;
using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;
using XppInterpreter.Parser.Metadata;

namespace XppInterpreter.Parser
{
    public class Variable : Expression
    {
        public string Name => (Token as Word).Lexeme;
        public Expression Caller { get; }
        public bool StaticCall { get; }
        public string Namespace { get; set; }
        public Variable(Word identifier, Expression caller, bool staticCall, string nameSpace, SourceCodeBinding sourceCodeBinding) : base(identifier, sourceCodeBinding)
        {
            Caller = caller;
            StaticCall = staticCall;
            Namespace = nameSpace;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitVariable(this);
        }

        internal override System.Type Accept(ITypeInferExpressionVisitor inferer)
        {
            return inferer.VisitVariable(this);
        }
    }
}
