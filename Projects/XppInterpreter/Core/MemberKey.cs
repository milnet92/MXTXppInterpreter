using System;

namespace XppInterpreter.Core
{
    /// <summary>
    /// The key of the caches of members: a type and the name of a member, whatever its case.
    /// What a type has doesn't change while the process runs, and a script asks about the same member on every iteration of a loop.
    /// </summary>
    internal struct MemberKey : IEquatable<MemberKey>
    {
        public readonly Type Type;
        public readonly string Name;
        public readonly bool Flag;

        /// <param name="flag">What else tells two lookups apart: non public members included, or static ones</param>
        public MemberKey(Type type, string name, bool flag = false)
        {
            Type = type;
            Name = name;
            Flag = flag;
        }

        public bool Equals(MemberKey other) =>
            Type == other.Type && Flag == other.Flag && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is MemberKey other && Equals(other);

        public override int GetHashCode() =>
            (Type.GetHashCode() * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Name) ^ (Flag ? 1 : 0);
    }
}
