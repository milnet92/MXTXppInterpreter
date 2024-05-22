using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Query;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Next : IInstruction
    {
        public string OperationCode => "NEXT";

        public void Execute(RuntimeContext context)
        {
            SearchInstance.ExecuteNext(context.Stack.Pop());
        }
    }
}
