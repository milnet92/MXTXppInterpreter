using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Data
{
    public class ValidTimeState
    {
        public Expression FromDate { get; }
        public Expression ToDate { get; }

        public ValidTimeState(Expression fromDate, Expression toDate)
        {
            FromDate = fromDate;
            ToDate = toDate;
        }
    }
}
