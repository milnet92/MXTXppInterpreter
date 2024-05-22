using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Data
{
    public class Where
    {
        public Expression Expression { get; }

        public Where(Expression expression)
        {
            Expression = expression;
        }
    }
}
