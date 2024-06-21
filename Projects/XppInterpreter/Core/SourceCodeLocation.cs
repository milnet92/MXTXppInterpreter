namespace XppInterpreter.Core
{
    public class SourceCodeLocation
    {
        public int Line { get; set; }
        public int Position { get; set; }

        public SourceCodeLocation(int line, int position)
        {
            Line = line;
            Position = position;
        }

        public override string ToString()
        {
            return $"Line: {Line}, Position {Position}";
        }
    }
}
