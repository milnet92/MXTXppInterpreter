using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Proxy
{
    public interface IIntrinsicFunctionProvider
    {
        int classNum(string name);
        string classStr(string name);
        string formStr(string name);
        string extendedTypeStr(string name);
        string menuItemActionStr(string name);
        string menuItemDisplayStr(string name);
        string menuItemOutputStr(string name);
        string methodStr(string className, string methodName);
        string staticMethodStr(string className, string methodName);
        int tableNum(string name);
        string tableStr(string name);
        object conNull();
        object dateNull();
        object maxDate();
        int maxInt();
        int minInt();
        int enumNum(string enumName);
    }
}
