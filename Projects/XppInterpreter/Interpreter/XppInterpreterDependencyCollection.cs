using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter
{
    public class XppInterpreterDependencyCollection : ICollection<Program>
    {
        private List<Program> _dependencyList = new List<Program>();

        public int Count => _dependencyList.Count();

        public bool IsReadOnly => false;

        public void Add(Program dependency)
        {
            if (!Contains(dependency))
            {
                _dependencyList.Add(dependency);
            }
        }

        public void Clear()
        {
            _dependencyList.Clear();
        }

        public bool Contains(Program dependency)
        {
            return _dependencyList.Contains(dependency);
        }

        public void CopyTo(Program[] array, int arrayIndex)
        {
            _dependencyList.CopyTo(array, arrayIndex);
        }

        public IEnumerator<Program> GetEnumerator()
        {
            return _dependencyList.GetEnumerator();
        }

        public bool Remove(Program item)
        {
            return _dependencyList.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dependencyList.GetEnumerator();
        }
    }
}
