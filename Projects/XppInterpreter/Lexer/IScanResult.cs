namespace XppInterpreter.Lexer
{
    /// <summary>
    /// Interface for the lexer result
    /// </summary>
    public interface IScanResult
    {
        Token Token { get; }
        int Start { get; }
        int End { get; }
        int Line { get; }
    }
}
