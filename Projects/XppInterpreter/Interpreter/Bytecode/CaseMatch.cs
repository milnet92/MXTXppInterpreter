namespace XppInterpreter.Interpreter.Bytecode
{
    /// <summary>
    /// A value of a case with many values (<c>case 1, 2:</c>) that is not the last one: when it matches, goes to the statements of the case.
    /// </summary>
    class CaseMatch : Jump
    {
        public override string OperationCode => "CASE_MATCH";

        public CaseMatch(int offset) : base(offset) { }

        public override void Execute(RuntimeContext context)
        {
            var right = context.Stack.Pop();
            var left = context.Stack.Peek();

            if (context.Proxy.Binary.AreEqual(left, right))
            {
                base.Execute(context);
            }
        }
    }
}
