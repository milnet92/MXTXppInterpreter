using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Completer
{
    class AutoCompleteInterruption : Exception
    {
        public Type InferedType { get; }

        public AutoCompleteInterruption(Type type)
        {
            InferedType = type;
        }
    }
}
