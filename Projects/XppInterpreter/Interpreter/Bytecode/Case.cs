using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Case : Jump
    {
        public override string OperationCode => "CASE";

        public Case(int offset) : base(offset) { }

        public override void Execute(RuntimeContext context)
        {
            var right = context.Stack.Pop();
            var left = context.Stack.Peek();

            if (!context.Proxy.Binary.AreEqual(left, right))
            {
                base.Execute(context);
            }
        }
    }
}
