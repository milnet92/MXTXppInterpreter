namespace XppInterpreter.Parser.Data
{
    public class ValidTimeState
    {
        public Expression FromDate { get; }
        public Expression ToDate { get; }

        /// <summary>validTimeState(asOf), as opposed to validTimeState(from, to)</summary>
        public bool AsOf => ReferenceEquals(FromDate, ToDate);

        public ValidTimeState(Expression fromDate, Expression toDate)
        {
            FromDate = fromDate;
            ToDate = toDate;
        }
    }
}
