using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter.Debug
{
    /// <summary>
    /// Interface that represents an element that can be debugged by <see cref="IDebugger"/>
    /// </summary>
    public interface IDebuggeable
    {
        SourceCodeBinding DebuggeableBinding { get; set; }
    }
}
