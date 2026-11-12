namespace XppInterpreter.Interpreter.Bytecode
{
    /// <summary>
    /// A while select that was left with a break, or by an exception, must not go on from where it was.
    /// </summary>
    class ResetQuery : IInstruction
    {
        public string OperationCode => "RESET_QUERY";
        public Parser.Data.Query Query { get; }

        public ResetQuery(Parser.Data.Query query)
        {
            Query = query;
        }

        public void Execute(RuntimeContext context)
        {
            context.Queries.Remove(Query);
        }
    }
}
