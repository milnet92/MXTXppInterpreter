using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Data
{
    public enum OrderByType
    {
        Unespecified,
        Ascending,
        Descending
    }

    public class OrderByField : Field
    {
        public OrderByType OrderByDirection { get; }

        public OrderByField(string tableName, string name, OrderByType orderByType = OrderByType.Unespecified) : base(tableName, name)
        {
            OrderByDirection = orderByType;
        }
    }
}
