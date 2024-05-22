using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppBinaryOperationProxy
    {
        object Add(object left, object right);
        object Substract(object left, object right);
        object Multiply(object left, object right);
        object Divide(object left, object right);
        object IntDivide(object left, object right);
        object Mod(object left, object right);
        bool Greater(object left, object right);
        bool AreEqual(object left, object right);
    }
}
