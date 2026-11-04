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
        public Type ClrType { get; }

        /// <summary>
        /// The n of <c>str n name;</c>
        /// </summary>
        public int StringLength { get; set; }

        /// <summary>
        /// The value goes to <c>record.(fieldId)</c>
        /// </summary>
        public bool IsFieldById { get; set; }

        public Store(string name, bool fromStack, bool top, bool isArray, string typeName, Type clrType)
        {
            Name = name;
            Top = top;
            FromStack = fromStack;
            IsArray = isArray;
            TypeName = typeName;
            ClrType = clrType;
        }

        internal virtual object GetValue(RuntimeContext context)
        {
            return context.Stack.Pop();
        }

        public void Execute(RuntimeContext context)
        {
            if (IsFieldById)
            {
                int fieldId = Convert.ToInt32(context.Stack.Pop());
                object record = context.Stack.Pop();

                context.Proxy.Reflection.SetFieldValueById(record, fieldId, GetValue(context));
                return;
            }

            int index = IsArray ? Load.ToArrayIndex(context.Stack.Pop()) : 0;

            if (FromStack)
            {
                var caller = context.Stack.Pop();
                var value = GetValue(context);

                if (IsArray)
                {
                    // Get the instance property array and assign the value to the corresponding index
                    var array = context.Proxy.Reflection.GetInstanceProperty(caller, Name);
                    context.Proxy.Casting.SetArrayIndexValue(array, index, value);
                }
                else if (caller is null || !context.Proxy.Reflection.IsXppType(caller.GetType()))
                {
                    Core.Clr.ClrBinder.SetMember(caller, Name, value);
                }
                else
                {
                    context.Proxy.Reflection.SetInstanceProperty(caller, Name, value);
                }
            }
            else
            {
                var value = GetValue(context);

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
                        if (value is null && context.Proxy.Reflection.IsCommonType(ClrType))
                        {
                            var defaultValue = context.Proxy.Casting.GetDefaultValueForType(TypeName);
                            context.ScopeHandler.CurrentScope.SetVar(Name, defaultValue, context.Proxy.Casting, Top, ClrType, TypeName, StringLength);
                        }
                        else
                        {
                            context.ScopeHandler.CurrentScope.SetVar(Name, value, context.Proxy.Casting, Top, ClrType, TypeName, StringLength);
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
                            context.ScopeHandler.CurrentScope.SetVar(Name, value, context.Proxy.Casting);
                        }
                    }
                }
            }
        }
    }
}
