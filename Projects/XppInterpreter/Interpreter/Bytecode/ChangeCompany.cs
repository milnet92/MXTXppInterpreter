using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    class ChangeCompany : IInstruction
    {
        public string OperationCode => "CHANGE_COMPANY";

        public void Execute(RuntimeContext context)
        {
            string company = (string)context.Stack.Pop();
            IDisposable changeCompanyHandler = context.Proxy.Data.CreateChangeCompanyHandler(company);
            context.ChangeCompanyHandlers.Push(changeCompanyHandler);
        }
    }
}
