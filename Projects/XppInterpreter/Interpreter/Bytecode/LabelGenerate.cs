using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Bytecode
{
    public static class Label
    {
        private static int _cnt = 0;

        public static string NewLabel()
        {
            _cnt ++;
            return $"L{_cnt}";
        }
    }
}
