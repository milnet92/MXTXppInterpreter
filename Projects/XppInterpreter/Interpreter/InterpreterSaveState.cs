using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter
{
    public class InterpreterSaveState
    {
        public RuntimeContext Context { get; }
        public Bytecode.ByteCode ByteCode { get; }
        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();

        public InterpreterSaveState(RuntimeContext runtimeContext, Bytecode.ByteCode byteCode)
        {
            Context = runtimeContext;
            ByteCode = byteCode;
        }

    }
}
