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

        /// <summary>
        /// The type of the variable of <c>catch (e)</c>, or null when it catches by Exception member
        /// </summary>
        public System.Type ClrType { get; set; }

        /// <summary>
        /// The variable of <c>catch (e)</c>
        /// </summary>
        public string VariableName { get; set; }

        public ExceptionCatchReference(string exceptionMemberName, int offset)
        {
            ExceptionMemberName = exceptionMemberName;
            Offset = offset;
        }
    }
}
