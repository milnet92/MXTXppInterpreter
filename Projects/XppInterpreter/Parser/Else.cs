using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser
{
    public class Else : If
    {
        public Else(Statement statement, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(new Constant(true, null), statement, null, sourceCodeBinding, debuggeableBinding) { }
    }
}
