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
