using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser
{
    internal class ParseContext
    {
        public ParseContextStack CallFunctionScope = new ParseContextStack();
    }
}
