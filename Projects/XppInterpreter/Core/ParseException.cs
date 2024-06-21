using System;
using XppInterpreter.Lexer;

namespace XppInterpreter.Core
{
    public class ParseException : Exception
    {
        public Token Token { get; set; }
        public int Line { get; set; }
        public int Position { get; set; }

        public ParseException(string message, Token token, int line, int position, bool showLine = false) : base(message + (showLine ? $" at line {(line + 1)}" : ""))
        {
            Token = token;
            Line = line;
            Position = position;
        }
    }
}
