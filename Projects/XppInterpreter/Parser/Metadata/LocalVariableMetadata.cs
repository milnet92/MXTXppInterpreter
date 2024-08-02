using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Metadata
{
    public class LocalVariableMetadata : TokenMetadata
    {
        public string VariableName { get; }
        public string DeclarationTypeName { get; }

        public LocalVariableMetadata(string type, string name)
        {
            DeclarationTypeName = type;
            VariableName = name;
        }

        public override string GetDisplayHtml()
        {
            return $"(local variable) {Span(DeclarationTypeName, COLOR_CSS_CYAN)} {VariableName}";
        }
    }
}
