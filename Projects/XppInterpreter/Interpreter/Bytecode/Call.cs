using System.Collections.Generic;

namespace XppInterpreter.Interpreter.Bytecode
{
    abstract class Call : ICall, IInstruction
    {
        public virtual string OperationCode => $"CALL {Name} {NArgs}" + (Alloc ? "(alloc)" : "(no alloc)");

        public string Name { get; }
        public int NArgs { get; }
        public bool Alloc { get; }
        public bool ProcessParameters { get; }

        /// <summary>
        /// The arguments passed with byref: the position of the argument and the variable that receives the value back.
        /// </summary>
        public List<KeyValuePair<int, string>> ByRefArguments { get; set; }

        /// <summary>
        /// The declared type of the arguments, when they are .NET variables.
        /// </summary>
        public System.Type[] ArgumentTypes { get; set; }

        public Call(string name, int nArgs, bool allocate, bool processParameters = true)
        {
            Name = name;
            NArgs = nArgs;
            Alloc = allocate;
            ProcessParameters = processParameters;
        }

        protected object[] GetParametersFromStack(Stack<object> stack)
        {
            object[] arguments = new object[NArgs];

            // Arguments are evaluated from left to right, so the last one is on top
            for (int narg = NArgs - 1; narg >= 0; narg--)
            {
                arguments[narg] = stack.Pop();
            }

            return arguments;
        }

        public void Execute(RuntimeContext context)
        {
            object returnValue;

            if (ProcessParameters)
            {
                var arguments = GetParametersFromStack(context.Stack);
                returnValue = MakeCall(context, arguments);

                if (ByRefArguments != null)
                {
                    foreach (var byRefArgument in ByRefArguments)
                    {
                        context.ScopeHandler.CurrentScope.SetVar(byRefArgument.Value, arguments[byRefArgument.Key], context.Proxy.Casting);
                    }
                }
            }
            else
            {
                returnValue = MakeCall(context, null);
            }

            if (Alloc)
            {
                context.Stack.Push(returnValue);
            }
        }

        public abstract object MakeCall(RuntimeContext context, object[] arguments);
    }
}
