using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Core
{
    public class VariableCollection : IEnumerable
    {
        private Dictionary<string, VariableEntry> _var = new Dictionary<string, VariableEntry>(StringComparer.InvariantCultureIgnoreCase);

        public VariableEntry this[string key] { get => _var[key]; set => _var[key] = value; }

        public void Remove(string key)
        {
            _var.Remove(key);
        }

        public void Add(VariableEntry entry)
        {
            _var.Add(entry.Name, entry);
        }

        public bool Exists(string name)
        {
            return _var.ContainsKey(name);
        }

        public IEnumerator GetEnumerator()
        {
            return _var.Values.GetEnumerator();
        }
    }
}
