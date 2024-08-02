namespace XppInterpreter.Parser
{
    public class ParseContext
    {
        private readonly ParseContextScope _globalScope = new ParseContextScope();
        public ParseContextScope CurrentScope { get; private set; }
        public ParseContextStack CallFunctionScope { get; } = new ParseContextStack();
        public ParseContextStack FunctionDeclarationStack { get; } = new ParseContextStack();

        public ParseContext()
        {
            CurrentScope = _globalScope;
        }

        public void BeginScope()
        {
            CurrentScope = CurrentScope.Begin();
        }

        public void EndScope()
        {
            if (CurrentScope.Parent != null)
            {
                CurrentScope = CurrentScope.End();
            }
        }
    }
}
