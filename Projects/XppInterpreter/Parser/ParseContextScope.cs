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

        public VariableDeclarations FindVariableDeclaration(string identifier)
        {
            ParseContextScope scope = this;

            do
            {
                var declaration = VariableDeclarations.FirstOrDefault(v => v.Identifiers.Keys.Any(k => k.Lexeme == identifier));

                if (declaration != null)
                {
                    return declaration;
                }

                scope = scope.Parent;
            } while (scope != null);

            return null;
        }
    }
}
