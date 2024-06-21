using System.Collections.Generic;

namespace XppInterpreter.Parser
{
    class ParseContextStack
    {
        private readonly Stack<bool> _stack = new Stack<bool>();

        public bool Empty => _stack.Count == 0;

        public void New()
        {
            _stack.Push(true);
        }

        public void Release()
        {
            _stack.Pop();
        }
    }
}
