using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    class ParseContextScope
    {
        public readonly List<VariableDeclarations> VariableDeclarations = new List<VariableDeclarations>();
        public ParseContextScope Parent {get; set; }
        public ParseContextScope Begin()
        {
            return new ParseContextScope() { Parent = this };
        }
        public bool IsGlobal()
        {
            return Parent == null;
        }

        public ParseContextScope End()
        {
            return Parent;
        }
    }
}
