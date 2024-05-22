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
    public abstract class Expression : ISourceCodeBindable, IAstNode, IDebuggeable
    {
        public Token Token { get; }
        public SourceCodeBinding SourceCodeBinding { get; private set; }
        public SourceCodeBinding DebuggeableBinding { get; set; }

        public Expression(Token token, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding = null)
        {
            SourceCodeBinding = sourceCodeBinding;
            Token = token;
            DebuggeableBinding = debuggeableBinding;
        }

        public abstract void Accept(IAstVisitor interpreter);
    }
}
