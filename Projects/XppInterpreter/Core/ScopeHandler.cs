using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Debug;

namespace XppInterpreter.Core
{
    public sealed class ScopeHandler
    {
        internal readonly Scope GlobalScope = new Scope();
        public Scope CurrentScope { get; private set; }

        public ScopeHandler()
        {
            CurrentScope = GlobalScope;
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
                CurrentScope = CurrentScope.End();
        }

        public VariableEditValueResponse TrySetVariableValueFromString(string name, string value)
        {
            // First check if variable exist
            Scope currentScope = CurrentScope;
            NormalizedScopeEntry foundEntry = null;

            while (currentScope != null)
            {
                var entries = currentScope.GetNormalizedScopeEntries(null);

                foundEntry = entries.FirstOrDefault(e => e.VariableName == name);

                if (foundEntry != null) break;

                currentScope = currentScope.Parent;
            }

            if (foundEntry is null) throw new Exception($"Variable {name} was not found.");

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

                CurrentScope.SetVar(name, parsedValue, false, false);
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
