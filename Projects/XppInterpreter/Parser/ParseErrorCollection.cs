using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser
{
    public class ParseErrorCollection : ICollection<ParseError>
    {
        private readonly List<ParseError> _list = new List<ParseError>();
        public int Count => _list.Count;

        public bool IsReadOnly =>  false;

        public void Add(ParseError item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(ParseError item)
        {
            return _list.Any(i => i == item);
        }

        public void CopyTo(ParseError[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ParseError> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public bool Remove(ParseError item)
        {
            return _list.RemoveAll(i => i == item) != 0;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
