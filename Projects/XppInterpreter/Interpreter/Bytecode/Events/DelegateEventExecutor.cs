namespace XppInterpreter.Interpreter.Bytecode.Events
{
    internal class DelegateEventExecutor
    {
        private RuntimeContext _context;
        private RefFunction _ref;

        public DelegateEventExecutor(RuntimeContext context, RefFunction refFunction)
        {
            _context = context;
            _ref = refFunction;
        }

        public void Execute()
        {
            ExecuteFunction();
        }

        public void Execute<T1>(T1 t1)
        {
            ExecuteFunction(t1);
        }

        public void Execute<T1, T2>(T1 t1, T2 t2)
        {
            ExecuteFunction(t1, t2);
        }

        public void Execute<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
        {
            ExecuteFunction(t1, t2, t3);
        }

        public void Execute<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4)
        {
            ExecuteFunction(t1, t2, t3, t4);
        }

        public void Execute<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            ExecuteFunction(t1, t2, t3, t4, t5);
        }

        public void Execute<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        {
            ExecuteFunction(t1, t2, t3, t4, t5, t6);
        }

        public void Execute<T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
        {
            ExecuteFunction(t1, t2, t3, t4, t5, t6, t7);
        }

        public void Execute<T1, T2, T3, T4, T5, T6, T7, T8>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
        {
            ExecuteFunction(t1, t2, t3, t4, t5, t6, t7, t8);
        }
        
        public void Execute<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9)
        {
            ExecuteFunction(t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void Execute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10)
        {
            ExecuteFunction(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void Execute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11)
        {
            ExecuteFunction(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void Execute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12)
        {
            ExecuteFunction(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void Execute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13)
        {
            ExecuteFunction(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void Execute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14)
        {
            ExecuteFunction(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void Execute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15)
        {
            ExecuteFunction(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void Execute<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16)
        {
            ExecuteFunction(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        private void ExecuteFunction(params object[] arguments)
        {
            ByteCode newByteCode = new ByteCode(_ref.Instructions)
            {
                DeclaredFunctions = _context.ByteCode.DeclaredFunctions
            };

            RuntimeContext newContext = new RuntimeContext(_context.Proxy, newByteCode);

            for (int numParam = 0; numParam < arguments.Length; numParam++)
            {
                var funcParameter = _ref.Declaration.Parameters[numParam];
                System.Type declarationType = _ref.Declaration.Parameters[numParam].DeclarationClrType;

                newContext.ScopeHandler.CurrentScope.SetVar(funcParameter.Name, arguments[numParam], _context.Proxy.Casting, true, declarationType);
            }

            XppInterpreter interpreter = new XppInterpreter(_context.Proxy, _context.Interpreter.Options);

            interpreter.Interpret(newContext.ByteCode, newContext, nextAction: Interpreter.Debug.DebugAction.None);
        }
    }
}
