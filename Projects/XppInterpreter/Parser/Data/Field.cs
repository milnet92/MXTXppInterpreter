using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Data
{
    public class Field
    {
        public string Name { get; }
        public string TableVarName { get; }

        public Field(string name)
        {
            Name = name;
        }

        public Field(string tableVarName, string name) : this(name)
        {
            TableVarName = tableVarName;
        }
    }
}
