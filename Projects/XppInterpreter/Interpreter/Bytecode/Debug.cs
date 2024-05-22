using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Debug : IInstruction
    {
        public string OperationCode => "DEBUG";
        public IDebuggeable Debuggeable { get; }
        public bool Always { get; }

        public Debug(IDebuggeable debuggeable, bool always = false)
        {
            Debuggeable = debuggeable;
            Always = always;
        }

        public void Execute(RuntimeContext context)
        {
            // NOOP
        }
    }
}
