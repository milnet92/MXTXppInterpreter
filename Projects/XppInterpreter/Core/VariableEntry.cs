using System;

namespace XppInterpreter.Core
{
    public class VariableEntry
    {
        public string Name { get; }
        public object Value { get; private set; }
        public Type DeclarationType { get; private set; }

        public VariableEntry(string name, object value)
        {
            Name = name;
            Value = value;

            if (value is null)
            {
                throw new Exception($"Variable {value} with no type cannot be null");
            }
        }

        public VariableEntry(string name, Type declarationType, object value)
        {
            Name = name;
            DeclarationType = declarationType;
            Value = value;
        }

        public void SetValue(object value)
        {
            // TODO: Check types
            Value = value;
        }
    }
}
