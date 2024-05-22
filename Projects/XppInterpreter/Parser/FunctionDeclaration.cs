using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class FunctionDeclaration : Statement
    {
        public Token ReturnType { get; }
        public string Name { get; }
        public List<FunctionParameter> Parameters { get; }

        public FunctionDeclaration(string name, Token returnType, List<FunctionParameter> parameters, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            ReturnType = returnType;
            Name = name;
            Parameters = parameters;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            // TODO: Implement interpreter
            throw new NotImplementedException();
        }
    }
}
