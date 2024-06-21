using System;

namespace XppInterpreter.Interpreter.Bytecode
{
    /// <summary>
    /// Used to check counters
    /// </summary>
    class MaxLoopThrow : IInstruction
    {
        public string OperationCode => "CHECK_LOOP_MAX";

        public void Execute(RuntimeContext context)
        {
            var value = (int)context.Stack.Pop();

            if (value == 0)
            {
                throw new Exception("Max loop counter has been reached.");
            }

            // Decrement counter
            context.Stack.Push(value - 1);
        }
    }
}
