using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class FunctionParameter
    {
        public Token Type { get; }
        public string Name { get; }

        public FunctionParameter(Token type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}
