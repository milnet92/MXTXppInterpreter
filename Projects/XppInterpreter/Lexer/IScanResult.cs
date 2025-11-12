namespace XppInterpreter.Lexer
{
    /// <summary>
    /// Interface for the lexer result
    /// </summary>
    public interface IScanResult
    {
        Token Token { get; }
        int Start { get; set; }
        int End { get; set; }
        int Line { get; set; }
    }
}
