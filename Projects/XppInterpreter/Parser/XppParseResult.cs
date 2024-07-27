using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser
{
    public class XppParseResult : IParseResult
    {
        public IAstNode AST { get; set; }
        public ParseErrorCollection Errors { get; }
        public bool IsCorrect => Errors.Count == 0;
        public bool HasErrors => Errors.Count != 0;

        public XppParseResult()
        {
            Errors = new ParseErrorCollection();
        }

        public XppParseResult(IAstNode ast) : this()
        {
            AST = ast;
        }

        public XppParseResult(IAstNode ast, ParseErrorCollection parseErrors)
        {
            AST = ast;
            Errors = parseErrors is null ? new ParseErrorCollection() : parseErrors;
        }

        public T ToNode<T>() where T : IAstNode
        {
            if (AST is null) return default(T);

            return (T)AST;
        }
    }
}
