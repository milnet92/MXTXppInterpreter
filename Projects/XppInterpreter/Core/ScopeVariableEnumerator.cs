using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Core
{
    internal class ScopeVariableEnumerator : IEnumerator<VariableEntry>
    {
        private Scope _scope = null;
        private int _currentIndex = -1;
        private List<VariableEntry> _variables = new List<VariableEntry>();
        public VariableEntry Current => _currentIndex < 0 ? null : _variables[_currentIndex];
        object IEnumerator.Current => Current;

        public ScopeVariableEnumerator(Scope scope)
        {
            _scope = scope;

            CollectVariables();
        }

        private void CollectVariables()
        {
            Scope currentScope = _scope;

            while (currentScope != null)
            {
                foreach (VariableEntry variableEntry in currentScope.VariableCollection)
                {
                    if (!_variables.Exists(v => v.Name == variableEntry.Name))
                    {
                        _variables.Add(variableEntry);
                    }
                }

                currentScope = currentScope.Parent;
            }
        }

        public void Reset()
        {
            _currentIndex = -1;
            _variables.Clear();
        }

        public void Dispose()
        {
            Reset();
            _scope = null;
        }

        public bool MoveNext()
        {
            if (_currentIndex + 1 < _variables.Count)
            {
                _currentIndex++;
                return true;
            }

            return false;
        }
    }
}
