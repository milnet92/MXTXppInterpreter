using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Completer
{
    public class CompletionCollection : IEnumerable<Completion>
    {
        private readonly List<Completion> _list = new List<Completion>();

        public int Count => _list.Count;

        public Completion GetAt(int index)
        {
            return _list[index];
        }

        public void Add(Completion completion)
        {
            _list.Add(completion);
        }
        public bool Remove(Completion completion)
        {
            return _list.Remove(completion);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public IEnumerator<Completion> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var completion in _list)
            {
                builder.AppendLine($"{completion.EntryType} : {completion.Value}");
            }

            return builder.ToString();
        }
    }
}
