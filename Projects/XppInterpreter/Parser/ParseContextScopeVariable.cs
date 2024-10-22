using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class ParseContextScopeVariable : ITypedObject
    {
        public string Name { get; }
        public ParsedTypeDefinition Type { get; }
        public Expression Initialization { get; }
        public bool IsArray { get; }
        public System.Type ClrType { get; }

        public ParseContextScopeVariable(string varName, ParsedTypeDefinition type, System.Type clrType, bool isArray, Expression initialization = null)
        {
            Name = varName;
            Type = type;
            IsArray = isArray;
            Initialization = initialization;
            ClrType = clrType;
        }
    }
}
