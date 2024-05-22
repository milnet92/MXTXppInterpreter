using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class VariableDeclarations : Statement
    {
        public Word VariableType { get; }
        public Dictionary<Word, Expression> Identifiers { get; }

        public VariableDeclarations(Word type, Dictionary<Word, Expression> identifiers, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            VariableType = type;
            Identifiers = identifiers; 

            // Invalidate debuggeable binding for idenfitiers
            foreach (var bindings in identifiers)
            {
                if (bindings.Value is IDebuggeable debuggeable)
                {
                    debuggeable.DebuggeableBinding = null;
                }
            }
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitVariableDeclarations(this);
        }
    }
}
