namespace XppInterpreter.Interpreter.Debug
{
    /// <summary>
    /// Stores the data for a new added and deleted <see cref="Breakpoint"/>
    /// </summary>
    public class BreakpointAction
    {
        public Breakpoint NewBreakpoint;
        public Breakpoint RemovedBreakpoint;

        public BreakpointAction(Breakpoint newBreakpoint, Breakpoint removedBreakpoint)
        {
            NewBreakpoint = newBreakpoint;
            RemovedBreakpoint = removedBreakpoint;
        }
    }
}
