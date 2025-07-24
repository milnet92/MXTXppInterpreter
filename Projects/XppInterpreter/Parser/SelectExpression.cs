using System;
using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Parser.Data;
using XppInterpreter.Parser.Metadata;

namespace XppInterpreter.Parser
{
    public class SelectExpression : Expression
    {
        public Query Query { get; }
        public string ReturnField { get; }
        public SelectExpression(Lexer.Token token, Query query, string returnField, SourceCodeBinding sourceCodeBinding) : base(token, sourceCodeBinding, sourceCodeBinding)
        {
            Query = query;
            ReturnField = returnField;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitSelectExpression(this);
        }

        internal override Type Accept(ITypeInferExpressionVisitor inferer)
        {
            return inferer.VisitSelectExpression(this);
        }
    }
}
