using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser
{
    internal class LoopControlContext : IDisposable
    {
        private readonly bool _needToReleaseLoop;
        private readonly bool _needToReleaseFinally;
        private readonly ParseContext _context;
        public LoopControlContext(ParseContext parseContext, bool isLoop = true)
        {
            _context = parseContext;
            _needToReleaseLoop = !isLoop || !_context.LoopStack.Empty;
            
            if (!isLoop)
            {
                _needToReleaseFinally = true;
                _context.FinallyStack.New();
            }

            _context.LoopStack.New(isLoop);
        }

        public void Dispose()
        {
            if (_needToReleaseLoop)
            {
                _context.LoopStack.Release();
            }

            if (_needToReleaseFinally)
            {
                _context.FinallyStack.Release();
            }
        }
    }
}
