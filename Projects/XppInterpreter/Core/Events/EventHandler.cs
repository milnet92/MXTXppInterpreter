using Microsoft.SqlServer.Server;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Bytecode;
using XppInterpreter.Interpreter.Bytecode.Events;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Lexer;
using XppInterpreter.Parser;

namespace XppInterpreter.Core.Events
{
    public class EventHandler
    {
        private RefFunction _function;
        XppProxy _proxy;

        public string SourceCode { get; }
        public string FieldName { get; }
        public System.Type DelegateType { get; }
        public System.Type ClassType { get; }
        public Delegate Delegate { get; private set; }
        public string Key => $"{ClassType.Namespace}_{ClassType.Name}_{FieldName}";

        public EventHandler(System.Type classType, System.Type delegateType, string fieldName, string code, XppProxy proxy)
        {
            SourceCode = code;
            DelegateType = delegateType;
            FieldName = fieldName;
            ClassType = classType;
            _proxy = proxy;
        }

        public FieldInfo GetField()
        {
            return ClassType.GetField(FieldName);
        }

        public void Compile(bool createDelegate)
        {
            var lexer = new XppLexer(SourceCode);
            var parser = new XppParser(lexer, _proxy);

            var interpreter = new Interpreter.XppInterpreter(_proxy);

            var parseResult = parser.Parse();

            if (parseResult.HasErrors)
                throw new Exception($"There is an error with the event handler code: {parseResult.Errors.First().Message}");

            ByteCode compiled = interpreter.Compile(parseResult.AST as Program);

            if (compiled.DeclaredFunctions.Count != 1)
                throw new Exception("Only one function is allowed to be present in event handler code");

            _function = compiled.DeclaredFunctions[0];

            if (_function.Declaration.Type.TokenType != TType.Void)
                throw new Exception("Event handler return type must be 'void'");

            EnsureDelegateParameters();

            if (createDelegate)
                CreateDelegate();
        }

        private void EnsureDelegateParameters()
        {
            if (_function is null) return;

            var parameterTypes = GetParameterTypes();

            if (parameterTypes.Length != _function.Declaration.Parameters.Count)
            {
                throw new Exception("Parameter count missmatch between handler and delegate.");
            }

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                var parameter = parameterTypes[i];
                var functionParameter = _function.Declaration.Parameters[i];

                if (parameter != functionParameter.DeclarationClrType)
                {
                    throw new Exception($"Parameter {functionParameter.Name} type is incorrect. Expected {parameter.Name} but got {functionParameter.DeclarationClrType.Name}");
                }
            }
        }

        private void CreateDelegate()
        {
            var parameterTypes = GetParameterTypes();
            var methodToExecute = DelegateHelper.GetMethodToExecute(this, parameterTypes);
            Delegate = Delegate.CreateDelegate(DelegateType, this, methodToExecute);
        }

        private System.Type[] GetParameterTypes()
        {
            return DelegateType.GetMethod("Invoke").GetParameters().Select(o => o.ParameterType).ToArray();
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

        public void ExecuteFunction(params object[] arguments)
        {
            ByteCode newByteCode = new ByteCode(_function.Instructions);

            RuntimeContext runtimeContext = new RuntimeContext(_proxy, newByteCode);

            for (int numParam = 0; numParam < arguments.Length; numParam++)
            {
                var funcParameter = _function.Declaration.Parameters[numParam];
                System.Type declarationType = _function.Declaration.Parameters[numParam].DeclarationClrType;

                runtimeContext.ScopeHandler.CurrentScope.SetVar(funcParameter.Name, arguments[numParam], runtimeContext.Proxy.Casting, true, declarationType);
            }

            Interpreter.XppInterpreter interpreter = new Interpreter.XppInterpreter(_proxy);

            interpreter.Interpret(runtimeContext.ByteCode, runtimeContext, nextAction: Interpreter.Debug.DebugAction.None);
        }
    }
}
