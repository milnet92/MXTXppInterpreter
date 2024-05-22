using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Data
{
    public class CrossCompany
    {
        public Expression Container { get; }

        public CrossCompany(Expression container = null)
        {
            Container = container;
        }
    }
}
