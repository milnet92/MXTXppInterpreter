﻿namespace XppInterpreter.Lexer
{
    /// <summary>
    /// XppLexer result implementation
    /// </summary>
    public class ScanResult : IScanResult
    {
        public Token Token { get; private set; }
        public int Start { get; set; }
        public int End { get; set; }
        public int Line { get; set; }

        public ScanResult(Token token, int start, int end, int line)
        {
            Token = token;
            Start = start;
            End = end;
            Line = line;
        }

        public override string ToString()
        {
            return $"{Token} {Line} {Start} {End}";
        }
    }
}
