using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
