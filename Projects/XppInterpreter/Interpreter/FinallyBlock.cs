using System;

namespace XppInterpreter.Interpreter
{
    /// <summary>
    /// What goes on when a running finally block ends. Each block has its own, because the block can have
    /// try statements of its own: they must not finish, or lose, what the block is holding.
    /// </summary>
    internal sealed class FinallyBlock
    {
        public FinallyBlock(Exception exception, PendingExit exit, int handlerCount)
        {
            Exception = exception;
            Exit = exit;
            HandlerCount = handlerCount;
        }

        public Exception Exception { get; }

        public PendingExit Exit { get; }

        /// <summary>
        /// The handlers still open around the block: an exception caught by one of the others leaves the block
        /// </summary>
        public int HandlerCount { get; }
    }
}
