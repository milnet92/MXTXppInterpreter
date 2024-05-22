using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Parser
{
    public abstract class Statement : ISourceCodeBindable, IAstNode, IDebuggeable
    {
        public Statement(SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding)
        {
            SourceCodeBinding = sourceCodeBinding;
            DebuggeableBinding = debuggeableBinding;
        }

        public SourceCodeBinding SourceCodeBinding { get; }
        public SourceCodeBinding DebuggeableBinding { get; set; }

        public abstract void Accept(IAstVisitor interpreter);
    }
}
