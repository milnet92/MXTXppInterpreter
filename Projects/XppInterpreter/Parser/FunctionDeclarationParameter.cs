using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class FunctionDeclarationParameter : Statement
    {
        public Token Type { get; }
        public string Name { get; }

        public FunctionDeclarationParameter(Token type, string name, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, null)
        {
            Type = type;
            Name = name;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            throw new NotImplementedException();
        }
    }
}
