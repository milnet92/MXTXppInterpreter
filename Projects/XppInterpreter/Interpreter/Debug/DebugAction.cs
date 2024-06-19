using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Debug
{
    /// <summary>
    /// Represents wha actions could be taken by the debug client
    /// whenever a breakpoint hits
    /// </summary>
    public enum DebugAction
    {
        None,
        Continue,
        StepOver,
        StopDebugging,
        CancelExecution,
    }
}
