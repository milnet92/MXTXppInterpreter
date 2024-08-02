using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class ParseContextScope
    {
        public readonly List<FunctionDeclaration> FunctionDeclarations = new List<FunctionDeclaration>();
        public readonly List<ParseContextScopeVariable> VariableDeclarations = new List<ParseContextScopeVariable>();
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

        public ParseContextScopeVariable FindVariableDeclaration(string identifier)
        {
            ParseContextScope scope = this;

            do
            {
                var declaration = scope.VariableDeclarations.FirstOrDefault(v => v.VariableName.ToLowerInvariant() == identifier.ToLowerInvariant());

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
