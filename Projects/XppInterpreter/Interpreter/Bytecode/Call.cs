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
        public string Namespace { get;  }
        public Call(string name, int nArgs, bool allocate, string nameSpace, bool processParameters = true)
        {
            Name = name;
            NArgs = nArgs;
            Alloc = allocate;
            ProcessParameters = processParameters;
            Namespace = nameSpace;
        }

        protected object[] GetParametersFromStack(Stack<object> stack)
        {
            object[] arguments = new object[NArgs];

            for (int narg = 0; narg < NArgs; narg++)
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
