using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser
{
    public class SourceCodeBinding
    {
        public readonly int FromLine;
        public readonly int ToLine;
        public readonly int FromPosition;
        public readonly int ToPosition;

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
