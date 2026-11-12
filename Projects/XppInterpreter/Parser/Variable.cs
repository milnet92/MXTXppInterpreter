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

        /// <summary>
        /// When this is the name of a .NET type, used to reach its static members: the type.
        /// </summary>
        public System.Type ClrType { get; set; }

        public bool IsByRef { get; set; }

        public bool IsSystemVariable { get; set; }

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

        internal override System.Type Accept(ITypeInferExpressionVisitor inferer)
        {
            return inferer.VisitVariable(this);
        }
    }
}
