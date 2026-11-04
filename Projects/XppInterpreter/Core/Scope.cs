using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Core
{
    public class Scope
    {
        internal readonly VariableCollection VariableCollection = new VariableCollection();
        internal readonly List<object> disposables = new List<object>();

        public readonly Stack<object> Stack = new Stack<object>();
        public Scope Parent { get; set; }

        public Scope Begin()
        {
            return new Scope() { Parent = this };
        }

        public bool IsGlobal()
        {
            return Parent == null;
        }

        public Scope End()
        {
            foreach (var obj in disposables)
            {
                IDisposable disposable = obj as IDisposable;

                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }

            return Parent;
        }

        public object GetVar(string varName)
        {
            if (VariableCollection.Exists(varName))
            {
                return VariableCollection[varName].Value;
            }
            else if (Parent != null)
            {
                return Parent.GetVar(varName);
            }

            throw new Exception($"Variable {varName} was not declared.");
        }

        public void SetVar(string varName, object varValue, Interpreter.Proxy.IXppCastingProxy castingProxy, bool forceOnTop = false, Type declarationType = null, string typeName = null, int stringLength = 0)
        {
            for (Scope current = this; current != null; current = current.Parent)
            {
                bool exists = current.VariableCollection.Exists(varName);

                if (exists)
                {
                    var entry = current.VariableCollection[varName];

                    entry.SetValue(ConvertToType(varValue, entry.DeclarationType, entry.TypeName, entry.StringLength, castingProxy));
                    return;
                }

                if (forceOnTop)
                {
                    if (declarationType is null)
                    {
                        break;
                    }

                    object value = ConvertToType(varValue, declarationType, typeName, stringLength, castingProxy);

                    current.VariableCollection.Add(new VariableEntry(varName, declarationType, value, typeName) { StringLength = stringLength });
                    return;
                }
            }

            throw new Exception($"Variable {varName} is not declared.");
        }

        /// <summary>
        /// A variable keeps its declared type: the value is converted to it, as the X++ compiler does on every assignment.
        /// </summary>
        private static object ConvertToType(object value, Type declarationType, string typeName, int stringLength, Interpreter.Proxy.IXppCastingProxy castingProxy)
        {
            // The debugger changes values that were already converted
            if (castingProxy is null) return value;

            object converted = Clr.ClrValues.CopyIfStruct(value);

            if (value is null)
            {
                // An X++ str is never null. A System.String variable can be.
                if (declarationType == typeof(string) && !string.IsNullOrEmpty(typeName))
                {
                    converted = string.Empty;
                }
                else if (declarationType != null && declarationType.IsValueType && Nullable.GetUnderlyingType(declarationType) is null)
                {
                    // A .NET nullable without value that goes to an int64, for example
                    converted = Activator.CreateInstance(declarationType);
                }
            }
            else if (declarationType != null && value.GetType() != declarationType)
            {
                converted = ConvertToDeclaredType(converted, declarationType, castingProxy);
            }

            if (converted is string edtValue && !string.IsNullOrEmpty(typeName) && castingProxy.IsStringEDT(typeName))
            {
                converted = castingProxy.TruncateEDTString(edtValue, typeName);
            }

            if (stringLength > 0 && converted is string text && text.Length > stringLength)
            {
                converted = text.Substring(0, stringLength);
            }

            return converted;
        }

        private static object ConvertToDeclaredType(object value, Type declarationType, Interpreter.Proxy.IXppCastingProxy castingProxy)
        {
            // The X++ value types convert with the X++ rules, unless they come from a .NET value that X++ doesn't know
            if (declarationType.IsInstanceOfType(value) || !IsXppValueType(value.GetType()) || !IsXppValueType(declarationType))
            {
                if (Clr.ClrConversions.TryConvert(value, declarationType, out object converted))
                {
                    return converted;
                }
            }

            return castingProxy.Cast(value, declarationType);
        }

        private static bool IsXppValueType(Type type) =>
            type == typeof(string) || type == typeof(int) || type == typeof(long) || type == typeof(decimal) || type == typeof(bool) || type == typeof(Guid);

        public NormalizedScopeEntry[] GetNormalizedScopeEntries(XppProxy proxy = null)
        {
            List<NormalizedScopeEntry> ret = new List<NormalizedScopeEntry>();

            foreach (VariableEntry variable in VariableCollection)
            {
                NormalizedScopeEntry entry = new NormalizedScopeEntry()
                {
                    VariableName = variable.Name,
                    Value = variable.Value,
                    TypeName = variable.DeclarationType is null ? variable.Value?.GetType().FullName ?? "" : variable.DeclarationType.FullName,
                };

                if (proxy != null && proxy.Reflection.IsEnum(entry.TypeName))
                {
                    entry.EnumValues = proxy.Reflection.GetAllEnumValues(entry.TypeName).Cast<string>().ToArray();
                }

                ret.Add(entry);
            }

            return ret.ToArray();
        }
    }
}
