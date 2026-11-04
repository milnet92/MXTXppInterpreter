namespace XppInterpreter.Interpreter
{
    /// <summary>
    /// A return, break, continue or retry that is on its way out of one or more try statements,
    /// and has to run their finally blocks first.
    /// </summary>
    internal sealed class PendingExit
    {
        public bool IsReturn { get; private set; }

        public bool IsRetry { get; private set; }

        /// <summary>
        /// Where a break or a continue goes when all the handlers are left
        /// </summary>
        public int TargetCounter { get; private set; }

        public int HandlersToLeave { get; set; }

        public static PendingExit Return(int handlersToLeave) =>
            new PendingExit { IsReturn = true, HandlersToLeave = handlersToLeave };

        public static PendingExit Retry(int handlersToLeave) =>
            new PendingExit { IsRetry = true, HandlersToLeave = handlersToLeave };

        public static PendingExit Jump(int targetCounter, int handlersToLeave) =>
            new PendingExit { TargetCounter = targetCounter, HandlersToLeave = handlersToLeave };
    }
}
