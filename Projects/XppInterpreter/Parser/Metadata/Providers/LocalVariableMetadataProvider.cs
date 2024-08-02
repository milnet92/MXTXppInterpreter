using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Parser.Metadata.Providers
{
    class LocalVariableMetadataProvider : ITokenMetadataProvider
    {
        public ParseContext ParseContext { get; }
        public string VariableName { get; }

        public LocalVariableMetadataProvider(string varName, ParseContext context)
        {
            VariableName = varName;
            ParseContext = context;
        }

        public TokenMetadata GetTokenMetadata(XppProxy proxy)
        {
            var declaration = ParseContext.CurrentScope.FindVariableDeclaration(VariableName);

            if (declaration is null)
            {
                return null;
            }
            else
            {
                return new LocalVariableMetadata(((Lexer.Word)declaration.Type).Lexeme, VariableName);
            }
        }
    }
}
