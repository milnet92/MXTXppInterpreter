using System;

namespace XppInterpreter.Interpreter.Bytecode
{
    class ChangeCompany : IInstruction
    {
        public string OperationCode => "CHANGE_COMPANY";

        public void Execute(RuntimeContext context)
        {
            string company = (string)context.Stack.Pop();
            IDisposable changeCompanyHandler = context.Proxy.Data.CreateChangeCompanyHandler(company);
            context.Disposables.Push(changeCompanyHandler);
        }
    }
}
