using System.Collections.Generic;
using System.Linq;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter.Debug
{
    /// <summary>
    /// Implementation for a debugger that can be attached to an <see cref="XppInterpreter"/>
    /// </summary>
    public class XppDebugger : IDebugger
    {
        private List<IDebuggeable> _bindableElements;
        private readonly AST2SourceCodeBindableCollection _bindableCollection;
        private readonly List<Breakpoint> _breakpoints = new List<Breakpoint>();
        public int BreakpointCount => _breakpoints.Count;

        public XppDebugger(Program program)
        {
            _bindableCollection = new AST2SourceCodeBindableCollection(program);
        }

        private void InitializeInternal()
        {
            if (_bindableElements is null)
            {
                _bindableElements = _bindableCollection.Generate();
            }
        }

        /// <summary>
        /// This function is used by the client to ask the debugger to add / remove a breakpoint.
        /// It will take care if the resulted action is delete, add or delete and add a breakpoint
        /// </summary>
        /// <param name="line">Source code line</param>
        /// <param name="position">Source code column</param>
        /// <returns>An instance of <see cref="BreakpointAction"/></returns>
        public BreakpointAction TryAddBreakpoint(int line, int position)
        {
            // Try first to remove at position
            Breakpoint removedBreakpoint = this.TryRemoveBreakpointAtLine(line);

            // Try to created by line
            Breakpoint newBreakpoint = this.TryAddBreakpointAtLine(line, position);

            if (newBreakpoint is null)
            {
                // Try now by line and column (position)
                newBreakpoint = this.TryAddBreakpointAtFilePosition(line, position);
            }

            if (newBreakpoint != null && !newBreakpoint.Equals(newBreakpoint, removedBreakpoint))
            {
                _breakpoints.Add(newBreakpoint);
            }
            else
            {
                newBreakpoint = null;
            }

            return new BreakpointAction(newBreakpoint, removedBreakpoint);
        }

        /// <summary>
        /// Clears all breakpoints that were added
        /// </summary>
        public void ClearAllBreakpoints()
        {
            _breakpoints.Clear();
        }

        /// <summary>
        /// Tries to remove a breakpoint for the given line and position
        /// </summary>
        /// <param name="line">Source code line</param>
        /// <param name="position">Source code position</param>
        /// <returns>An instance of <see cref="Breakpoint"/>. If no breakpoints was found, returns <c>null</c>.</returns>
        internal Breakpoint TryRemoveBreakpointAtLine(int line)
        {
            Breakpoint existingBreakpoint = _breakpoints.FirstOrDefault(
                bp =>
                bp.SourceCodeMapping.From.Line <= line &&
                bp.SourceCodeMapping.To.Line >= line);

            if (existingBreakpoint != null)
            {
                _breakpoints.Remove(existingBreakpoint);
            }

            return existingBreakpoint;
        }

        /// <summary>
        /// Tries to add a breakpoint priorizing the first element of a line
        /// </summary>
        /// <param name="line">Source code line</param>
        /// <param name="position">Source code position</param>
        /// <returns>An instnace of <see cref="Breakpoint"/>. If no breakpoint was added, returns <c>null</c>.</returns>
        internal Breakpoint TryAddBreakpointAtLine(int line, int position)
        {
            InitializeInternal();

            Breakpoint breakpoint = null;
            IDebuggeable debuggeable = null;

            foreach (var bindableElement in _bindableElements.Where(element => element.DebuggeableBinding.FromLine == line))
            {
                if (debuggeable is null)
                {
                    debuggeable = bindableElement;
                }

                var binding = bindableElement.DebuggeableBinding;

                if (binding.FromPosition <= position &&
                    ((binding.ToLine == line && binding.ToPosition >= position) ||
                     (binding.ToLine > line)))
                {
                    debuggeable = bindableElement;
                    break;
                }
            }

            if (debuggeable != null)
            {
                var binding = debuggeable.DebuggeableBinding;

                breakpoint = new Breakpoint(new Core.SourceCodeRange(
                    new Core.SourceCodeLocation(binding.FromLine, binding.FromPosition),
                    new Core.SourceCodeLocation(binding.ToLine, binding.ToPosition)),
                    debuggeable);
            }

            return breakpoint;
        }

        /// <summary>
        /// Tries to add a breakpoint priorizing the exact position
        /// </summary>
        /// <param name="line">Source code line</param>
        /// <param name="position">Source code position</param>
        /// <returns>An instnace of <see cref="Breakpoint"/>. If no breakpoint was added, returns <c>null</c>.</returns>
        internal Breakpoint TryAddBreakpointAtFilePosition(int line, int position)
        {
            InitializeInternal();

            IDebuggeable debuggeable = null;
            Breakpoint breakpoint = null;

            foreach (var debuggeableElementByLine in _bindableElements.Where(
                element =>
                element.DebuggeableBinding.FromLine <= line &&
                element.DebuggeableBinding.ToLine >= line))
            {
                var binding = debuggeableElementByLine.DebuggeableBinding;

                if (binding.FromLine == line)
                {
                    debuggeable = binding.FromPosition <= position ? debuggeableElementByLine : debuggeable;
                }
                else if (binding.ToLine == line)
                {
                    debuggeable = binding.ToPosition >= position ? debuggeableElementByLine : debuggeable;
                }
                else
                {
                    debuggeable = debuggeableElementByLine;
                }
            }

            if (debuggeable != null)
            {
                var binding = debuggeable.DebuggeableBinding;

                breakpoint = new Breakpoint(new Core.SourceCodeRange(
                    new Core.SourceCodeLocation(binding.FromLine, binding.FromPosition),
                    new Core.SourceCodeLocation(binding.ToLine, binding.ToPosition)),
                    debuggeable);
            }

            return breakpoint;
        }

        /// <summary>
        /// Gets the <see cref="Breakpoint"/> (if any) by an instnace of <see cref="IDebuggeable"/>
        /// </summary>
        /// <param name="debuggeable"><see cref="IDebuggeable"/> instance</param>
        /// <returns>An instance of <see cref="Breakpoint"/>. If none is found, returns <c>null</c>.</returns>
        public Breakpoint GetBreakpointAtElement(IDebuggeable debuggeable)
        {
            return _breakpoints.FirstOrDefault(bp => bp.Element == debuggeable);
        }
    }
}
