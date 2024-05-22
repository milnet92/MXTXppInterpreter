using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            SearchInstance searchInstance = null;
            bool found = false;

            if (UseAsExpression && context.Queries.TryGetValue(Query, out searchInstance))
            {
                found = searchInstance.Next();
            }
            else
            {
                searchInstance = new QueryGenerator(context).NewSearchInstance(Query);
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
