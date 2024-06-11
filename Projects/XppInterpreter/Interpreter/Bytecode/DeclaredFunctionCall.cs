using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    internal class DeclaredFunctionCall : Call
    {
        private RefFunction _ref;

        public DeclaredFunctionCall(RefFunction refFunction) : base(refFunction.Declaration.Name, refFunction.Declaration.Parameters.Count, false, true)
        {
            _ref = refFunction;
        }

        public override object MakeCall(RuntimeContext context, object[] arguments)
        {
            throw new NotImplementedException();
        }
    }
}
