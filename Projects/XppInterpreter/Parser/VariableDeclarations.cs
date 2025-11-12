using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class VariableDeclarations : Statement
    {
        public System.Type DeclarationClrType { get; }
        public ParsedTypeDefinition DeclarationType { get; }
        public Dictionary<Word, Expression> Identifiers { get; }

        public VariableDeclarations(ParsedTypeDefinition declarationType, System.Type declarationClrType, Dictionary<Word, Expression> identifiers, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            DeclarationType = declarationType;
            Identifiers = identifiers;
            DeclarationClrType = declarationClrType;

            // Invalidate debuggeable binding for idenfitiers
            foreach (var bindings in identifiers)
            {
                if (bindings.Value is IDebuggeable debuggeable)
                {
                    debuggeable.DebuggeableBinding = null;
                }
            }
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitVariableDeclarations(this);
        }
    }
}
