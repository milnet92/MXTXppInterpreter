namespace XppInterpreter.Lexer
{
    /// <summary>
    /// Interface for the lexer result
    /// </summary>
    public interface IScanResult
    {
        Token Token { get; }

        /// <summary>
        /// The text of a keyword as it was written: the token of a keyword has one spelling for all of them, and System.Int64 needs the original one.
        /// </summary>
        string Text { get; }
        int Start { get; }
        int End { get; }
        int Line { get; }
    }
}
