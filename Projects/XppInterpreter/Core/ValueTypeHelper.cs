using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Core
{
    public static class ValueTypeHelper
    {
        public static bool IsNullable<T>(T t) { return false; }
        public static bool IsNullable<T>(T? t) where T : struct { return true; }
    }
}
