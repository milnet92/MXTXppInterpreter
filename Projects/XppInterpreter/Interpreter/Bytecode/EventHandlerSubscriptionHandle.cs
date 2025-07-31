using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using XppInterpreter.Core;
using XppInterpreter.Core.Events;
using XppInterpreter.Interpreter.Bytecode.Events;

namespace XppInterpreter.Interpreter.Bytecode
{
    internal class EventHandlerSubscriptionHandle : IInstruction
    {
        public string OperationCode => "EVENT_SUBSCRIPTION";

        public RefFunction FunctionReference{ get; }

        public EventHandlerSubscriptionHandle(RefFunction refFunction)
        {
            FunctionReference = refFunction;
        }

        public void Execute(RuntimeContext context)
        {
            EventSubscriptionHandle handle = (EventSubscriptionHandle)context.Stack.Pop();

            // Get delegate field and type
            FieldInfo delegateField = ReflectionHelper.GetField(handle.Instance.GetType(), handle.MethodName);
            Type[] parameterTypes = delegateField.FieldType.GetMethod("Invoke").GetParameters().Select(o =>  o.ParameterType).ToArray();
            
            DelegateEventExecutor executor = new DelegateEventExecutor(context, FunctionReference);
            var methodToExecute = DelegateHelper.GetMethodToExecute(executor, parameterTypes);

            Delegate executorDelegate = Delegate.CreateDelegate(delegateField.FieldType, executor, methodToExecute);

            // Get the list of subscribers
            var instanceDelegate = (MulticastDelegate)delegateField.GetValue(handle.Instance);

            // Combine them
            Delegate combinedDelegate = Delegate.Combine(instanceDelegate, executorDelegate);
            delegateField.SetValue(handle.Instance, combinedDelegate);
        }
    }
}
