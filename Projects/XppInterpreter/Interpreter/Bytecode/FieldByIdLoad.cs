namespace XppInterpreter.Interpreter.Bytecode
{
    class FieldByIdLoad : IInstruction
    {
        public string OperationCode => "LOAD_FIELD_BY_ID";

        public void Execute(RuntimeContext context)
        {
            int fieldId = System.Convert.ToInt32(context.Stack.Pop());
            object record = context.Stack.Pop();

            context.Stack.Push(context.Proxy.Reflection.GetFieldValueById(record, fieldId));
        }
    }
}
