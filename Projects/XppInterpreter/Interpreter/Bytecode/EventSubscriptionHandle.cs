using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    internal class EventSubscriptionHandle
    {
        public object Instance { get; }
        public string MethodName { get; set; }

        public EventSubscriptionHandle(object instance, string methodName)
        {
            Instance = instance;
            MethodName = methodName;
        }
    }
}
