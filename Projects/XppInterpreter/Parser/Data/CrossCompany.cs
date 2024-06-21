namespace XppInterpreter.Parser.Data
{
    public class CrossCompany
    {
        public Expression Container { get; }

        public CrossCompany(Expression container = null)
        {
            Container = container;
        }
    }
}
