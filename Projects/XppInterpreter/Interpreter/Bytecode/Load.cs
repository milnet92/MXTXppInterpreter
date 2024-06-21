using System;

namespace XppInterpreter.Interpreter.Bytecode
{
    abstract class Load : IInstruction
    {
        public string Name { get; }
        public virtual string OperationCode => $"LOAD {Name}";
        public bool IsArray { get; }
        public Load(string name, bool isArray)
        {
            Name = name;
            IsArray = isArray;
        }

        public virtual void Execute(RuntimeContext context)
        {
            object value = null;

            if (IsArray)
            {
                object index = context.Stack.Pop();

                if (index is null || index.GetType() != typeof(int))
                {
                    throw new Exception($"Invalid array index type {(index is null ? "null" : index.GetType().Name)}");
                }

                value = MakeLoadFromArray(context, (int)index);
            }
            else
            {
                value = MakeLoad(context);
            }

            context.Stack.Push(value);
        }

        public abstract object MakeLoad(RuntimeContext context);
        public abstract object MakeLoadFromArray(RuntimeContext context, int index);
    }
}
