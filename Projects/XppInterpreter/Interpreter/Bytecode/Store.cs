using System;

namespace XppInterpreter.Interpreter.Bytecode
{
    class Store : IInstruction
    {
        public string Name { get; }
        public string OperationCode
        {
            get
            {
                if (FromStack)
                {
                    return $"STORE_PROP {Name}";
                }
                else
                {
                    return $"STORE {Name}";
                }
            }
        }
        public bool Top { get; }
        public bool FromStack { get; }
        public bool IsArray { get; }
        public string TypeName { get; }

        public Store(string name, bool fromStack, bool top, bool isArray, string typeName)
        {
            Name = name;
            Top = top;
            FromStack = fromStack;
            IsArray = isArray;
            TypeName = typeName;
        }

        public void Execute(RuntimeContext context)
        {
            int index = IsArray ? (int)context.Stack.Pop() : 0;

            if (FromStack)
            {
                var caller = context.Stack.Pop();
                var value = context.Stack.Pop();

                if (IsArray)
                {
                    // Get the instance property array and assign the value to the corresponding index
                    var array = context.Proxy.Reflection.GetInstanceProperty(caller, Name);
                    context.Proxy.Casting.SetArrayIndexValue(array, index, value);
                }
                else
                {
                    context.Proxy.Reflection.SetInstanceProperty(caller, Name, value);
                }
            }
            else
            {
                var value = context.Stack.Pop();

                if (IsArray)
                {
                    var array = context.ScopeHandler.CurrentScope.GetVar(Name);
                    context.Proxy.Casting.SetArrayIndexValue(array, index, value);
                }
                else
                {
                    bool declaration = Top; // for the sake of clarity

                    if (declaration)
                    {
                        var defaultValue = context.Proxy.Casting.GetDefaultValueForType(TypeName);
                        Type declarationType = context.Proxy.Casting.GetSystemTypeFromTypeName(TypeName);

                        if (value is null && context.Proxy.Reflection.IsCommonType(declarationType))
                        {
                            context.ScopeHandler.CurrentScope.SetVar(Name, defaultValue, declaration, Top, declarationType);
                        }
                        else
                        {
                            context.ScopeHandler.CurrentScope.SetVar(Name, value, declaration, Top, declarationType);
                        }
                    }
                    else
                    {
                        var currentValue = context.ScopeHandler.CurrentScope.GetVar(Name);
                        var type = currentValue?.GetType();

                        if (value is null && context.Proxy.Reflection.IsCommonType(type))
                        {
                            context.Proxy.Reflection.ClearCommon(currentValue);
                        }
                        else
                        {
                            context.ScopeHandler.CurrentScope.SetVar(Name, value, false);
                        }
                    }
                }
            }
        }
    }
}
