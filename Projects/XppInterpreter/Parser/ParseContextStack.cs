using System.Collections.Generic;

namespace XppInterpreter.Parser
{
    class ParseContextStack : ParseContextStack<bool>
    {
        public bool Empty => _stack.Count == 0;

        public void New()
        {
            New(true);
        }
    }

    class ParseContextStack<T>
    {
        protected readonly Stack<T> _stack = new Stack<T>();

        public void New(T value)
        {
            _stack.Push(value);
        }

        public T Release()
        {
            return _stack.Pop();
        }
    }
}
