using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class FunctionDeclaration : Statement
    {
        public Token ReturnType { get; }
        public string Name { get; }
        public List<FunctionDeclarationParameter> Parameters { get; }
        public Block Block { get; }

        public FunctionDeclaration(string name, Token returnType, List<FunctionDeclarationParameter> parameters, Block block, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, null)
        {
            ReturnType = returnType;
            Name = name;
            Parameters = parameters;
            Block = block;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitFunctionDeclaration(this);
        }
    }
}
