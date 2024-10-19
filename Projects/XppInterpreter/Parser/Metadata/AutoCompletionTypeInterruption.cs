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

    public class AutoCompletionTypeInterruption : Exception
    {
        public Type InferedType { get; }
        public string Namespace { get; }

        public AutoCompletionTypeInterruption(Type type)
        {
            InferedType = type;
        }

        public AutoCompletionTypeInterruption(string @namespace)
        {
            Namespace = @namespace;
        }
    }
}
