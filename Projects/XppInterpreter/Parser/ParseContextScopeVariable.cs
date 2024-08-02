using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class ParseContextScopeVariable
    {
        public string VariableName { get; }
        public Token Type { get; }
        public Expression Initialization { get; }
        public bool IsArray { get; }

        public ParseContextScopeVariable(string varName, Token type, bool isArray, Expression initialization = null)
        {
            VariableName = varName;
            Type = type;
            IsArray = isArray;
            Initialization = initialization;
        }
    }
}
