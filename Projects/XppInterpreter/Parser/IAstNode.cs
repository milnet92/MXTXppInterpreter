using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public interface IAstNode
    {
        void Accept(IAstVisitor interpreter);
    }
}
