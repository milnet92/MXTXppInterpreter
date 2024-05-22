using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Core;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter.Debug
{
    /// <summary>
    /// Stores data regarding which main <see cref="IDebuggeable"/> element is attached
    /// and the mapping to the source code
    /// </summary>
    public class Breakpoint : IEqualityComparer<Breakpoint>
    {
        public SourceCodeRange SourceCodeMapping { get; set; }
        public IDebuggeable Element { get; set; }

        public Breakpoint(SourceCodeRange map, IDebuggeable element)
        {
            SourceCodeMapping = map;
            Element = element;
        }

        public Breakpoint(SourceCodeBinding sourceCodeBinding, IDebuggeable element)
        {
            SourceCodeMapping = new SourceCodeRange(
                new SourceCodeLocation(sourceCodeBinding.FromLine, sourceCodeBinding.FromPosition),
                new SourceCodeLocation(sourceCodeBinding.ToLine, sourceCodeBinding.ToPosition));
            Element = element;
        }

        public bool Equals(Breakpoint x, Breakpoint y)
        {

            if ((x is null && y != null) ||
                (x != null && y is null))
            {
                return false;
            }

            return 
                x.SourceCodeMapping.From.Line == y.SourceCodeMapping.From.Line &&
                x.SourceCodeMapping.From.Position == y.SourceCodeMapping.From.Position &&
                x.SourceCodeMapping.To.Line == y.SourceCodeMapping.To.Line &&
                x.SourceCodeMapping.To.Position == y.SourceCodeMapping.To.Position;
        }

        public int GetHashCode(Breakpoint obj)
        {
            return obj.Element.GetHashCode() + obj.SourceCodeMapping.GetHashCode();
        }
    }
}
