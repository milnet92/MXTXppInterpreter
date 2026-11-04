using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    internal sealed class ParsedType
    {
        public Word Word { get; }

        public IScanResult Start { get; }

        public System.Type ClrType { get; }

        public ParsedType(Word word, IScanResult start, System.Type clrType)
        {
            Word = word;
            Start = start;
            ClrType = clrType;
        }
    }
}
