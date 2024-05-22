using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppExceptionsProxy
    {
        void Throw(object obj);
    }
}
