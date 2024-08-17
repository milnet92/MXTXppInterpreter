using System;
using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class FunctionDeclarationParameter
    {
        public Word DeclarationType { get; }
        public System.Type DeclarationClrType { get; }
        public string Name { get; }

        public FunctionDeclarationParameter(Word declarationType, System.Type clrType, string name, SourceCodeBinding sourceCodeBinding)
        {
            DeclarationType = declarationType;
            Name = name;
            DeclarationClrType = clrType;
        }
    }
}
