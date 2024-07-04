using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Query;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppQueryGenerationProxy
    {
        IQueryGenerator NewQueryGenerator();
    }
}
