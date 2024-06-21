using System;
using System.Diagnostics;
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

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            throw new NotImplementedException();
        }
    }
}
