using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser
{
    public class ParseResult
    {
        public IAstNode AST { get; set; }
        public ParseErrorCollection Errors { get; }
        public bool IsCorrect => Errors.Count == 0;
        public bool HasErrors => Errors.Count != 0;

        public ParseResult()
        {
            Errors = new ParseErrorCollection();
        }

        public ParseResult(IAstNode ast) : this()
        {
            AST = ast;
        }

        public ParseResult(IAstNode ast, ParseErrorCollection parseErrors)
        {
            AST = ast;
            Errors = parseErrors is null ? new ParseErrorCollection() : parseErrors;
        }
    }
}
