using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser
{
    internal class LoopControlContext : IDisposable
    {
        private readonly bool _needToDispose;
        private readonly ParseContext _context;

        public LoopControlContext(ParseContext parseContext, bool isLoop = true)
        {
            _context = parseContext;
            _needToDispose = !isLoop || !_context.LoopStack.Empty;

            _context.LoopStack.New(isLoop);
        }

        public void Dispose()
        {
            if (_needToDispose)
            {
                _context.LoopStack.Release();
            }
        }
    }
}
