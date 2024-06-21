namespace XppInterpreter.Core
{
    public class SourceCodeRange
    {
        public SourceCodeLocation From { get; set; }
        public SourceCodeLocation To { get; set; }

        public SourceCodeRange(SourceCodeLocation from, SourceCodeLocation to)
        {
            From = from;
            To = to;
        }
    }
}
