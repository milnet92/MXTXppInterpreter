using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class FunctionDeclaration : Statement, ITypedObject
    {
        public ParsedTypeDefinition Type { get; }
        public string Name { get; }
        public List<FunctionDeclarationParameter> Parameters { get; }
        public Block Block { get; }

        public FunctionDeclaration(string name, ParsedTypeDefinition returnType, List<FunctionDeclarationParameter> parameters, Block block, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, null)
        {
            Type = returnType;
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
