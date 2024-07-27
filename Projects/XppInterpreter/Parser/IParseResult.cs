using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser
{
    public interface IParseResult
    {
        IAstNode AST { get; }
        ParseErrorCollection Errors { get; }
        T ToNode<T>() where T : IAstNode;
    }
}
