using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser
{
    internal class CatchContext : IDisposable
    {
        private readonly ParseContext _context;

        public CatchContext(ParseContext parseContext)
        {
            _context = parseContext;

            _context.CatchStack.New();
        }

        public void Dispose()
        {
            _context.CatchStack.Release();
        }
    }
}
