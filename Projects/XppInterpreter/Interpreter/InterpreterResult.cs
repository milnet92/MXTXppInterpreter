using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Interpreter
{
    public class InterpreterResult
    {
        public static InterpreterResult Finished = new InterpreterResult();
        public InterpreterSaveState SaveState { get; }
        public Breakpoint Breakpoint { get; }
        public bool HasStopped => Breakpoint != null;
        public bool HasFinished => Breakpoint is null;
        /// <summary>
        /// The value of the return statement that ended the code, or null
        /// </summary>
        public object ReturnValue { get; }

        public InterpreterResult(Breakpoint breakpoint, InterpreterSaveState saveState)
        {
            Breakpoint = breakpoint;
            SaveState = saveState;
        }

        public InterpreterResult() { }

        internal static InterpreterResult FinishedWith(object returnValue) =>
            returnValue is null ? Finished : new InterpreterResult(returnValue);

        private InterpreterResult(object returnValue)
        {
            ReturnValue = returnValue;
        }
    }
}
