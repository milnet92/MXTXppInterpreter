using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class ParseError : IEqualityComparer<ParseError>
    {
        public Token Token { get; }
        public int Line { get; }
        public int Position { get; }
        public string Message { get; }

        public ParseError(Token token, int line, string message)
        {
            Token = token;
            Line = line;
            Message = message;
        }

        public ParseError(Token token, int line, int position, string message) : this(token, line, message)
        {
            Position = position;
        }

        public bool Equals(ParseError x, ParseError y)
        {
            return x.Line == y.Line && x.Position == y.Position;
        }

        public int GetHashCode(ParseError obj)
        {
            return (obj.Line.GetHashCode()  * 17 + obj.Position.GetHashCode() * 17);
        }
    }
}
