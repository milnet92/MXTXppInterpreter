using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter.Bytecode
{
    public interface IInstruction
    {
        string OperationCode { get; }
        void Execute(RuntimeContext context);
    }
}
