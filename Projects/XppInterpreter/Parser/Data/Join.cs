using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Data
{
    public enum JoinType
    {
        Regular,
        Outer,
        Exists,
        NotExists
    }

    public class Join
    {
        public JoinType JoinType { get; }
        public Query Select { get; }

        public Join(JoinType joinType, Query select)
        {
            JoinType = joinType;
            Select = select;
        }

    }
}
