using System.Collections.Generic;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class VariableArrayDeclaration : VariableDeclarations
    {
        public Expression Size { get; }

        public VariableArrayDeclaration(ParsedTypeDefinition type, System.Type clrType, Word identifier, Expression size, SourceCodeBinding sourceCodeBinding) : base(type, clrType, new Dictionary<Word, Expression>() { { identifier, null } }, sourceCodeBinding)
        {
            Size = size;
        }
    }
}
