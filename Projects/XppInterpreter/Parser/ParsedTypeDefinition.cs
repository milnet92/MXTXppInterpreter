using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    internal class ParsedTypeDefinition
    {
        public IScanResult TypeResult { get; }
        public string Namespace { get; }
        public bool IsExternalType => !string.IsNullOrEmpty(Namespace);
        public ParsedTypeDefinition(IScanResult typeResult, string @namespace)
        {
            TypeResult = typeResult;
            Namespace = @namespace;
        }

        public override string ToString()
        {
            string typeName = ((Word)TypeResult.Token).Lexeme;

            if (!string.IsNullOrEmpty(Namespace))
                return $"{Namespace}.{typeName}";

            return $"{typeName}";
        }
    }
}
