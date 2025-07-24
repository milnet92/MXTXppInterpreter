using XppInterpreter.Interpreter.Query;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Select : IInstruction
    {
        public string OperationCode => "SELECT_QUERY";
        public Parser.Data.Query Query { get; }
        public bool IsWhileSelect { get; }
        public string ReturnField { get; }
        public Select(Parser.Data.Query query, bool isWhileSelect = false, string returnField = "")
        {
            Query = query;
            IsWhileSelect = isWhileSelect;
            ReturnField = returnField;
        }

        public void Execute(RuntimeContext context)
        {
            ISearchInstance searchInstance = null;
            bool found = false;

            if (!string.IsNullOrEmpty(ReturnField))
            {
                // Create the temporary buffer to handle the record
                object buffer = context.Proxy.Casting.GetDefaultValueForType(Query.TableVariableName);
                searchInstance = context.Proxy.QueryGeneration.NewQueryGenerator().NewSearchInstance(Query, context, true, buffer);
                searchInstance.Next();

                object returnValue = context.Proxy.Reflection.GetInstanceProperty(buffer, ReturnField);
                context.Stack.Push(returnValue);
            }
            else if (IsWhileSelect && context.Queries.TryGetValue(Query, out searchInstance))
            {
                found = searchInstance.Next();
            }
            else
            {
                searchInstance = context.Proxy.QueryGeneration.NewQueryGenerator().NewSearchInstance(Query, context, false, null);
                found = searchInstance.Next();
            }

            if (IsWhileSelect)
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
