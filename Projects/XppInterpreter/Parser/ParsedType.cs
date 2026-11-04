using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    /// <summary>
    /// The type name in a declaration, a <c>new</c>, an <c>is</c> or an <c>as</c>.
    /// </summary>
    internal sealed class ParsedType
    {
        /// <summary>
        /// The name as it was written
        /// </summary>
        public Word Word { get; }

        /// <summary>
        /// The first token of the name
        /// </summary>
        public IScanResult Start { get; }

        /// <summary>
        /// The .NET type, or null for an X++ type
        /// </summary>
        public System.Type ClrType { get; }

        public ParsedType(Word word, IScanResult start, System.Type clrType)
        {
            Word = word;
            Start = start;
            ClrType = clrType;
        }
    }
}
