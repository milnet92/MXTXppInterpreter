using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    public interface ICall
    {
        string Name { get; }
        int NArgs { get; }
        bool Alloc { get; }
        bool ProcessParameters { get; }
    }
}
