using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class ParsedTypeDefinition
    {
        public IScanResult TypeResult { get; }
        public string Namespace { get; }
        public string TypeName { get; }
        public bool IsExternalType => !string.IsNullOrEmpty(Namespace);
        public ParsedTypeDefinition(IScanResult typeResult, string @namespace)
        {
            TypeResult = typeResult;
            TypeName = ((Word)TypeResult.Token).Lexeme;
            Namespace = @namespace;
        }

        public ParsedTypeDefinition(string typeName, string @namespace)
        {
            TypeName = typeName;
            Namespace = @namespace;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Namespace))
                return $"{Namespace}.{TypeName}";

            return $"{TypeName}";
        }
    }
}
