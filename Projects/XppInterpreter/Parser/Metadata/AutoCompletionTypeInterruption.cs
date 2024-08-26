using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Metadata
{
    public enum AutoCompletionPurpose
    {
        InstanceMembers,
        StaticMembers,
        TableIndexes,
        TableFields
    }

    class AutoCompletionTypeInterruption : Exception
    {
        public Type InferedType { get; }

        public AutoCompletionTypeInterruption(Type type)
        {
            InferedType = type;
        }
    }
}
