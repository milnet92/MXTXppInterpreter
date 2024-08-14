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
        public readonly List<FunctionDeclarationReference> FunctionReferences = new List<FunctionDeclarationReference>();
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
                var declaration = scope.VariableDeclarations.FirstOrDefault(v => v.Name.ToLowerInvariant() == identifier.ToLowerInvariant());

                if (declaration != null)
                {
                    return declaration;
                }

                scope = scope.Parent;
            } while (scope != null);

            return null;
        }

        public FunctionDeclarationReference FindFunctionDeclarationReference(string functionName)
        {
            ParseContextScope scope = this;

            do
            {
                var declaration = scope.FunctionReferences.FirstOrDefault(v => v.Name.ToLowerInvariant() == functionName.ToLowerInvariant());

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
