using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;
using XppInterpreter.Parser.Metadata;

namespace XppInterpreter.Parser
{
    public class EventHandler
    {
        public string FunctionName { get; }

        public EventHandler(string functionName)
        {
            FunctionName = functionName;
        }
    }
}
