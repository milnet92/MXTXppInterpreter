namespace XppInterpreter.Interpreter.Bytecode
{
    class Negation : IInstruction
    {
        public string OperationCode => "NEG";

        public void Execute(RuntimeContext context)
        {
            var value = context.Stack.Pop();
            context.Stack.Push(context.Proxy.Unary.Negate(value));
        }
    }
}
