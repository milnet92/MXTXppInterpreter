namespace XppInterpreter.Parser
{
    internal class ParseContext
    {
        public ParseContextStack CallFunctionScope { get; } = new ParseContextStack();
        public ParseContextStack FunctionDeclarationStack { get; } = new ParseContextStack();
    }
}
