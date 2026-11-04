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
                value = MakeLoadFromArray(context, ToArrayIndex(context.Stack.Pop()));
            }
            else
            {
                value = MakeLoad(context);
            }

            context.Stack.Push(value);
        }

        /// <summary>
        /// Any integer type or enumeration can index an array
        /// </summary>
        internal static int ToArrayIndex(object index)
        {
            switch (index)
            {
                case int value: return value;
                case long value: return (int)value;
                case System.Enum value: return Convert.ToInt32(value);
                default:
                    throw new Exception($"Invalid array index type {(index is null ? "null" : index.GetType().Name)}");
            }
        }

        public abstract object MakeLoad(RuntimeContext context);
        public abstract object MakeLoadFromArray(RuntimeContext context, int index);
    }
}
