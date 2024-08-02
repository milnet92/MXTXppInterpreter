using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Metadata
{
    public enum TokenMetadataType
    {
        InstanceMethod,
        InstanceVariable,
        StaticMethod,
        IntrinsicMethod,
        Constructor,
        GlobalOrDefinedMethod,
        Type,
        Variable,
        Label
    }
}
