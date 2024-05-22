using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Lexer
{
    /// <summary>
    /// Interface for the lexer
    /// </summary>
    public interface ILexer
    {
        IScanResult GetNextToken();
        IScanResult Peek(int offset = 0);
    }
}
