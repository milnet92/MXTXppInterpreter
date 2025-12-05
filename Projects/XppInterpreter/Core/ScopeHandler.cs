using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Core
{
    public sealed class ScopeHandler
    {
        private readonly Stack<ExceptionHandler> _handlerStack = new Stack<ExceptionHandler>();

        internal readonly Scope GlobalScope = new Scope();
        public Scope CurrentScope { get; private set; }
        public ExceptionHandler CurrentExceptionHandler => AreExceptionsHandled ? _handlerStack.Peek() : null;
        public bool AreExceptionsHandled => _handlerStack.Count > 0;
        private readonly Interpreter.Proxy.XppProxy _proxy;

        public ScopeHandler(Interpreter.Proxy.XppProxy proxy)
        {
            CurrentScope = GlobalScope;
            _proxy = proxy;
        }

        private object InternalGetVar(Scope scope, string varName)
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }

            if (string.IsNullOrEmpty(varName))
            {
                throw new ArgumentNullException("varName");
            }

            return scope.GetVar(varName);
        }

        public object GetVar(string varName)
        {
            return InternalGetVar(CurrentScope, varName);
        }

        public void BeginScope()
        {
            CurrentScope = CurrentScope.Begin();
        }

        public void EndScope()
        {
            if (CurrentScope.Parent != null)
            {
                CurrentScope = CurrentScope.End();
            }
        }

        public object GetDisposableByType(Type type)
        {
            return CurrentScope.disposables.Where(d => d.GetType() == type).FirstOrDefault();
        }

        public void AddDisposable(object disposable)
        {
            CurrentScope.disposables.Add(disposable);
        }

        public void AddExceptionHandler(ExceptionHandler exceptionHandler)
        {
            _handlerStack.Push(exceptionHandler);
        }

        public ExceptionHandler RemoveExceptionHandler()
        {
            if (AreExceptionsHandled)
            {
                return _handlerStack.Pop();
            }

            return null;
        }


        public void EndScope(int num)
        {
            for (int i = 0; i < num; i++)
            {
                EndScope();
            }
        }

        public VariableEditValueResponse TrySetVariableValueFromString(string path, string newDisplayValue, string typeName)
        {
            VariableEntryEditor entryEditor = new VariableEntryEditor(this, _proxy);

            try
            {
                object newValue = entryEditor.ChangeValue(path, newDisplayValue, typeName);

                return new VariableEditValueResponse()
                {
                    Value = DebugHelper.GetDebugDisplayValue(newValue),
                    TypeName = newValue?.GetType().FullName,
                    Error = ""
                };
            }
            catch (Exception ex)
            {
                return new VariableEditValueResponse()
                {
                    Value = newDisplayValue,
                    TypeName = typeName,
                    Error = ex.Message
                };
            }
        }
    }
}
