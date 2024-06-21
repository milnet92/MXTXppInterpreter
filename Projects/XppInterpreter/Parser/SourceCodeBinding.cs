using System.Text;

namespace XppInterpreter.Parser
{
    public class SourceCodeBinding
    {
        public int FromLine { get; }
        public int ToLine { get; }
        public int FromPosition { get; }
        public int ToPosition { get; }

        public SourceCodeBinding(int fromLine, int fromPosition, int toLine, int toPosition)
        {
            FromLine = fromLine;
            ToLine = toLine;
            FromPosition = fromPosition;
            ToPosition = toPosition;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"From line: { FromLine}");
            builder.AppendLine($"From pos: { FromPosition}");
            builder.AppendLine($"To line: { ToLine}");
            builder.AppendLine($"To pos: { ToPosition}");

            return builder.ToString();
        }
    }
}
