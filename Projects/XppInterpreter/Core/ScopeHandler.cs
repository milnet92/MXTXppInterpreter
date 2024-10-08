﻿using System;
using System.Collections.Generic;
using System.Linq;
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

        public VariableEditValueResponse TrySetVariableValueFromString(string name, string value)
        {
            Scope currentScope = CurrentScope;
            NormalizedScopeEntry foundEntry = null;

            while (currentScope != null)
            {
                var entries = currentScope.GetNormalizedScopeEntries(null);

                foundEntry = entries.FirstOrDefault(e => e.VariableName == name);

                if (foundEntry != null)
                {
                    break;
                }

                currentScope = currentScope.Parent;
            }

            if (foundEntry is null)
            {
                throw new Exception($"Variable {name} was not found.");
            }

            if (!foundEntry.Editable)
            {
                return new VariableEditValueResponse()
                {
                    Value = DebugHelper.GetDebugDisplayValue(foundEntry.Value),
                    Error = $"Type {foundEntry.TypeName} not editable"
                };
            }

            bool valid = false;
            object parsedValue = null;

            switch (foundEntry.TypeName)
            {
                case "System.Int32":
                    if (int.TryParse(value, out int parsedInt))
                    {
                        parsedValue = parsedInt;
                        valid = true;
                    }
                    break;
                case "System.Decimal":
                    if (decimal.TryParse(value, out decimal parsedDecimal))
                    {
                        parsedValue = parsedDecimal;
                        valid = true;
                    }
                    break;
                case "System.String":
                    parsedValue = value;
                    valid = true;
                    break;
                case "System.Boolean":
                    if (bool.TryParse(value, out bool parsedBool))
                    {
                        parsedValue = parsedBool;
                        valid = true;
                    }
                    break;
                case "System.Int64":
                    if (Int64.TryParse(value, out Int64 parsedLong))
                    {
                        parsedValue = parsedLong;
                        valid = true;
                    }
                    break;
            }

            if (valid)
            {
                string normalizedValue = DebugHelper.GetDebugDisplayValue(parsedValue);

                CurrentScope.SetVar(name, parsedValue, _proxy.Casting, false);
                CurrentScope._hash.Update(name, normalizedValue);

                return new VariableEditValueResponse()
                {
                    Value = DebugHelper.GetDebugDisplayValue(parsedValue),
                    Error = ""
                };
            }
            else
            {
                return new VariableEditValueResponse()
                {
                    Value = DebugHelper.GetDebugDisplayValue(foundEntry.Value),
                    Error = $"Invalid {value} value for type {foundEntry.TypeName}"
                };
            }
        }
    }
}
