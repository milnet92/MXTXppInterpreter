namespace XppInterpreter.Interpreter.Bytecode
{
    /// <summary>
    /// The right operand of &amp;&amp; and ||: the result of the operator is a boolean, whatever the type of the operand.
    /// </summary>
    class ToBooleanValue : IInstruction
    {
        public string OperationCode => "TO_BOOLEAN";

        public void Execute(RuntimeContext context)
        {
            context.Stack.Push(Core.Clr.ClrValues.ToBoolean(context.Stack.Pop(), context.Proxy));
        }
    }
}
