using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Parser.Metadata;

namespace XppInterpreter.Parser
{
    public class TableField : Expression
    {
        public string TableName { get; }
        public string FieldName { get; }

        public TableField(Lexer.Token token, string tableName, string fieldName, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding = null) : base(token, sourceCodeBinding, debuggeableBinding)
        {
            TableName = tableName;
            FieldName = fieldName;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitTableField(this);
        }

        internal override Type Accept(ITypeInferExpressionVisitor inferer)
        {
            return inferer.VisitTableField(this);
        }
    }
}
