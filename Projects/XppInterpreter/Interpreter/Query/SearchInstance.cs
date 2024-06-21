namespace XppInterpreter.Interpreter.Query
{
    public class SearchInstance
    {
        public Dynamics.AX.Application.SysDaSearchObject _searchObject;
        public Dynamics.AX.Application.SysDaSearchStatement _searchStatement;
        public bool IsNextExecuted { get; private set; }
        public bool LastNextExecutionResult { get; private set; }

        public SearchInstance(Dynamics.AX.Application.SysDaSearchObject searchObject, Dynamics.AX.Application.SysDaSearchStatement searchStatement)
        {
            _searchObject = searchObject;
            _searchStatement = searchStatement;
        }

        public bool Next()
        {
            IsNextExecuted = true;
            LastNextExecutionResult = _searchStatement.findNext(_searchObject);
            return LastNextExecutionResult;
        }

        public static void ExecuteNext(object record)
        {
            Microsoft.Dynamics.Ax.Xpp.Common common = record as Microsoft.Dynamics.Ax.Xpp.Common;
            common.Next();
        }
    }
}
