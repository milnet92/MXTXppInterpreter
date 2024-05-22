using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser
{
    class ParseContextStack
    {
        private Stack<bool> _stack = new Stack<bool>();
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
