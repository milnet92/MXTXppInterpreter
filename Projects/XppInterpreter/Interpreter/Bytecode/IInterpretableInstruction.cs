using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    public interface IInterpretableInstruction
    {
        InterpreterResult LastResult { get; }
    }
}
