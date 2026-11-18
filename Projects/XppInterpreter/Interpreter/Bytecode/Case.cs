namespace XppInterpreter.Interpreter.Bytecode
{
    class Case : Jump
    {
        public override string OperationCode => "CASE";

        public Case(int offset) : base(offset) { }

        public override void Execute(RuntimeContext context)
        {
            var right = context.Stack.Pop();
            var left = context.Stack.Peek();

            if (!(Core.SimpleOperands.TryAreEqual(left, right, out bool equal) ? equal : context.Proxy.Binary.AreEqual(left, right)))
            {
                base.Execute(context);
            }
        }
    }
}
