namespace XppInterpreter.Parser.Data
{
    public class Where
    {
        public Expression Expression { get; }

        public Where(Expression expression)
        {
            Expression = expression;
        }
    }
}
