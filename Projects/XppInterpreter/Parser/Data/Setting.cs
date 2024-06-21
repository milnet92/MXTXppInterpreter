namespace XppInterpreter.Parser.Data
{
    public class Setting
    {
        public string FieldName { get; }
        public Expression Expression { get; }

        public Setting(string fieldName, Expression expression)
        {
            FieldName = fieldName;
            Expression = expression;
        }
    }
}
