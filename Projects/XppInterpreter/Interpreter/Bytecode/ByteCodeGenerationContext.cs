using System.Collections.Generic;

namespace XppInterpreter.Interpreter.Bytecode
{
    class ByteCodeGenerationContext
    {
        private List<Parser.Select> _selectsInWhile = new List<Parser.Select>();
        
        public void AddWhileSelect(Parser.Select select)
        {
            _selectsInWhile.Add(select);
        }

        public void RemoveWhileSelect(Parser.Select select)
        {
            _selectsInWhile.Remove(select);
        }

        public bool IsWhileSelectBeingGenerated(Parser.Select select)
        {
            return _selectsInWhile.Exists(s => s == select);
        }
    }
}
