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
        public Word DeclarationType { get; }
        public Dictionary<Word, Expression> Identifiers { get; }

        /// <summary>
        /// The n of <c>str n name;</c>. Zero when the type has no length.
        /// </summary>
        public int StringLength { get; set; }

        /// <summary>
        /// The arrays of the same statement: <c>int a[2], b, c[3];</c>
        /// </summary>
        public List<VariableArrayDeclaration> ArrayDeclarations { get; set; } = new List<VariableArrayDeclaration>();

        /// <summary>
        /// The declared type is a .NET type
        /// </summary>
        public bool IsClrType { get; set; }

        public VariableDeclarations(Word declarationType, System.Type declarationClrType, Dictionary<Word, Expression> identifiers, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
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
