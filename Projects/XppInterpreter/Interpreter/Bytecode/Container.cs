namespace XppInterpreter.Interpreter.Bytecode
{
    class Container : IInstruction
    {
        public string OperationCode => $"CONTAINER {NElements}";
        public int NElements { get; }

        public Container(int nElements)
        {
            NElements = nElements;
        }

        public void Execute(RuntimeContext context)
        {
            object[] elements = new object[NElements];

            // Elements are evaluated from left to right, so the last one is on top
            for (int nElement = NElements - 1; nElement >= 0; nElement--)
            {
                elements[nElement] = context.Stack.Pop();
            }

            context.Stack.Push(elements);
        }
    }
}
