using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class ExceptionCatchReference
    {
        public string ExceptionMemberName { get; }
        public int Offset { get; }

        public System.Type ClrType { get; set; }

        public string VariableName { get; set; }

        public ExceptionCatchReference(string exceptionMemberName, int offset)
        {
            ExceptionMemberName = exceptionMemberName;
            Offset = offset;
        }
    }
}
