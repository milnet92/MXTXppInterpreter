using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;
using XppInterpreter.Parser.Metadata;

namespace XppInterpreter.Parser
{
    /// <summary>
    /// A table field chosen when the script runs: <c>record.(fieldId)</c>. Its type is not known before that.
    /// </summary>
    public class FieldByIdAccess : Variable
    {
        private static readonly Word Identifier = new Word("(field)", TType.Id);

        public Expression FieldId { get; }

        public FieldByIdAccess(Expression caller, Expression fieldId, SourceCodeBinding sourceCodeBinding)
            : base(Identifier, caller, false, sourceCodeBinding)
        {
            FieldId = fieldId;
        }

        internal override System.Type Accept(ITypeInferExpressionVisitor inferer)
        {
            return null;
        }
    }
}
