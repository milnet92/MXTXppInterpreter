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

            for (int nElement = 0; nElement < NElements; nElement++)
            {
                elements[nElement] = context.Stack.Pop();
            }

            context.Stack.Push(elements);
        }
    }
}
