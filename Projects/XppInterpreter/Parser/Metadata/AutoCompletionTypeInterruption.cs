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

        /// <summary>
        /// The position is in the name of this .NET namespace
        /// </summary>
        public string Namespace { get; }

        public AutoCompletionTypeInterruption(string @namespace)
        {
            Namespace = @namespace;
        }

        public AutoCompletionTypeInterruption(Type type)
        {
            InferedType = type;
        }
    }
}
