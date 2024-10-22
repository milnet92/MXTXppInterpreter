using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class FunctionDeclarationReference : ITypedObject
    {
        public string Name { get; }
        public ParsedTypeDefinition Type { get; }
        public ParsedTypeDefinition ReturnType => Type;

        public FunctionDeclarationReference(string name, ParsedTypeDefinition type)
        {
            Name = name;
            Type = type;
        }
    }
}
