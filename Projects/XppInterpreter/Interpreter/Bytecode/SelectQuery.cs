using XppInterpreter.Interpreter.Query;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Select : IInstruction
    {
        public string OperationCode => "SELECT_QUERY";
        public Parser.Data.Query Query { get; }
        public bool UseAsExpression { get; }

        public Select(Parser.Data.Query query, bool useAsExpression)
        {
            Query = query;
            UseAsExpression = useAsExpression;
        }

        public void Execute(RuntimeContext context)
        {
            ISearchInstance searchInstance = null;
            bool found = false;

            if (UseAsExpression && context.Queries.TryGetValue(Query, out searchInstance))
            {
                found = searchInstance.Next();
            }
            else
            {
                searchInstance = context.Proxy.QueryGeneration.NewQueryGenerator().NewSearchInstance(Query, context);
                found = searchInstance.Next();
            }

            if (UseAsExpression)
            {
                if (!found)
                {
                    context.Queries.Remove(Query);
                }
                else
                {
                    context.Queries[Query] = searchInstance;
                }

                context.Stack.Push(found);
            }
        }
    }
}
