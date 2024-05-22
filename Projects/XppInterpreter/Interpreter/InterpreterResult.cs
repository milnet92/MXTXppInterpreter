using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Interpreter
{
    public class InterpreterResult
    {
        public static InterpreterResult Finished = new InterpreterResult();
        public InterpreterSaveState SaveState { get; }
        public Breakpoint Breakpoint { get; }
        public bool HasStopped => Breakpoint != null;
        public bool HasFinished => Breakpoint is null;

        public InterpreterResult(Breakpoint breakpoint, InterpreterSaveState saveState)
        {
            Breakpoint = breakpoint;
            SaveState = saveState;
        }

        public InterpreterResult() { }
    }
}
