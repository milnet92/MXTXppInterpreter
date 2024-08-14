using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    interface ITypedObject
    {
        string Name { get; }
        Token Type { get; }
    }
}
