using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Debug
{
    public class NormalizedScopeEntryHash
    {
        private readonly Dictionary<string, string> _table = new Dictionary<string, string>();

        public void Clear()
        {
            _table.Clear();
        }

        public void Update(string name, string value)
        {
            _table[name] = value;
        }

        public void Update(List<NormalizedScopeEntry> entries)
        {
            Clear();

            foreach (var entry in entries)
            {
                _table.Add(entry.VariableName, DebugHelper.GetDebugDisplayValue(entry.Value));
            }
        }

        public bool HasChanged(string variableName, object value)
        {
            if (!_table.ContainsKey(variableName)) return false;

            return _table[variableName] != DebugHelper.GetDebugDisplayValue(value);
        }
    }
}
