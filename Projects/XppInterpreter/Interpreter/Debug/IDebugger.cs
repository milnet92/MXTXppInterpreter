using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Debug
{
    /// <summary>
    /// Interface to implement the functionality for a debugger that
    /// can be attached to an interpreter
    /// </summary>
    public interface IDebugger
    {
        BreakpointAction TryAddBreakpoint(int line, int position);
        void ClearAllBreakpoints();
        Breakpoint GetBreakpointAtElement(IDebuggeable debuggeable);
    }
}
