namespace XppInterpreter.Lexer
{
    /// <summary>
    /// Interface for the lexer
    /// </summary>
    public interface ILexer
    {
        IScanResult GetNextToken();
        IScanResult Peek(int offset = 0);
    }
}
