using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace XppInterpreter.Core
{
    public class VariableCollection : IEnumerable
    {
        // Created with the first variable. Every block is a scope and most blocks declare nothing: a read that walks up
        // through them to find its variable has nothing to look up there.
        private Dictionary<string, VariableEntry> _var;

        // Names have no case in X++. An ordinal comparison is several times faster than one with culture,
        // and every read and write of a variable makes one.
        private Dictionary<string, VariableEntry> Variables =>
            _var ?? (_var = new Dictionary<string, VariableEntry>(StringComparer.OrdinalIgnoreCase));

        public VariableEntry this[string key] { get => Variables[key]; set => Variables[key] = value; }

        public void Remove(string key)
        {
            _var?.Remove(key);
        }

        public void Add(VariableEntry entry)
        {
            Variables.Add(entry.Name, entry);
        }

        public bool Exists(string name)
        {
            return _var != null && _var.ContainsKey(name);
        }

        public bool TryGet(string name, out VariableEntry entry)
        {
            if (_var is null)
            {
                entry = null;
                return false;
            }

            return _var.TryGetValue(name, out entry);
        }

        public IEnumerator GetEnumerator()
        {
            return (_var is null ? Enumerable.Empty<VariableEntry>() : _var.Values).GetEnumerator();
        }
    }
}
