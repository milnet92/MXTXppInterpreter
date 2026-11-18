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
        public Expression DefaultValue { get; }


        public FunctionDeclarationParameter(Word declarationType, System.Type clrType, string name, SourceCodeBinding sourceCodeBinding, Expression defaultValue = null)
        {
            DeclarationType = declarationType;
            Name = name;
            DeclarationClrType = clrType;
            DefaultValue = defaultValue;
        }
    }
}
