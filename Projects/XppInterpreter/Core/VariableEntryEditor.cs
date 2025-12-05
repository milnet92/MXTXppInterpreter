using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Core
{
    internal class VariableEntryEditor
    {
        private ScopeHandler _scopeHandler { get; }
        private XppProxy _proxy { get; }
        private BindingFlags _defaultBindingFlag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public VariableEntryEditor(ScopeHandler scopeHandler, XppProxy proxy)
        {
            _scopeHandler = scopeHandler;
            _proxy = proxy;
        }

        private NormalizedScopeEntry FindRootEntry(string varName)
        {
            Scope currentScope = _scopeHandler.CurrentScope;
            NormalizedScopeEntry foundEntry = null;

            while (currentScope != null)
            {
                var entries = currentScope.GetNormalizedScopeEntries(null);

                foundEntry = entries.FirstOrDefault(e => e.VariableName == varName);

                if (foundEntry != null)
                {
                    return foundEntry;
                }

                currentScope = currentScope.Parent;
            }

            return null;
        }

        private bool IsIndexer(string segment)
        {
            return segment.EndsWith("]") && segment.StartsWith("[");
        }

        private bool IsMetadata(string segment)
        {
            return segment == "Private Members" || segment == "Base Fields" || segment == "Static Members";
        }

        public object ChangeValue(string path, string value, string typeName)
        {
            if (value != null && ((value.StartsWith("\"") && !value.EndsWith("\"") || value.EndsWith("\"") && !value.StartsWith("\""))))
            {
                throw new InvalidOperationException("String value is not properly quoted.");
            }

            object newValue = null;
            Type type = string.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName);

            string[] segments = path.Split('.');
            string rootName = segments[0];
            NormalizedScopeEntry rootEntry = FindRootEntry(rootName);

            if (rootEntry is null)
            {
                throw new InvalidOperationException($"Variable '{rootName}' not found in the scope.");
            }

            if (path == rootName)
            {
                newValue = ParseValue(value, type);
                _scopeHandler.CurrentScope.SetVar(rootName, newValue, null);
                return newValue;
            }

            for (int i = 1; i < segments.Length - 1; i++)
            {
                string segment = segments[i];
                object currentValue = rootEntry.Value;

                if (currentValue == null)
                {
                    throw new InvalidOperationException($"Cannot navigate through null value at segment '{segment}'.");
                }

                if (IsIndexer(segment))
                {
                    // Get the index value
                    int  index = Int32.Parse(segment.Substring(1, segment.Length - 2));
                    Type currentValueType = currentValue.GetType();

                    if (currentValueType.FullName == "System.Object[]")
                    {
                        var arrayValue = (object[])currentValue;
                        rootEntry = new NormalizedScopeEntry
                        {
                            VariableName = segment,
                            Value = arrayValue[index - 1],
                            TypeName = arrayValue[index - 1]?.GetType().FullName
                        };
                        continue;
                    }
                    else
                    {
                        var indexerProperty = currentValueType.GetProperty("Item", new Type[] { typeof(int) });

                        if (indexerProperty != null)
                        {
                            rootEntry = new NormalizedScopeEntry
                            {
                                VariableName = segment,
                                Value = indexerProperty.GetValue(currentValue, new object[] { index }),
                                TypeName = indexerProperty.PropertyType.FullName
                            };
                            continue;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Indexer not found on type '{currentValueType.FullName}'.");
                        }
                    }
                }
                else if (!IsMetadata(segment))
                {
                    Type currentValueType = currentValue.GetType();

                    // Try with property
                    var propertyInfo = currentValueType.GetProperty(segment, _defaultBindingFlag);
                    if (propertyInfo != null)
                    {
                        rootEntry = new NormalizedScopeEntry
                        {
                            VariableName = segment,
                            Value = propertyInfo.GetValue(currentValue),
                            TypeName = propertyInfo.PropertyType.FullName
                        };
                        continue;
                    }

                    // Try with field
                    var fieldInfo = currentValueType.GetField(segment, _defaultBindingFlag);
                    if (fieldInfo != null)
                    {
                        rootEntry = new NormalizedScopeEntry
                        {
                            VariableName = segment,
                            Value = fieldInfo.GetValue(currentValue),
                            TypeName = fieldInfo.FieldType.FullName
                        };
                        continue;
                    }
                }
            }

            string lastSegment = segments[segments.Length - 1];
            object finalValue = rootEntry.Value;

            if (IsIndexer(lastSegment))
            {
                Type currentValueType = finalValue.GetType();
                int  index = Int32.Parse(lastSegment.Substring(1, lastSegment.Length - 2));

                newValue = ParseValue(value, null);

                if (currentValueType.FullName == "System.Object[]")
                {
                    var objArray = (object[])finalValue;
                    objArray[index - 1] = newValue;
                }
                else
                {
                    var indexerProperty = currentValueType.GetProperty("Item", new Type[] { typeof(int) });
                    indexerProperty.SetValue(finalValue, newValue, new object[] { index - 1 });
                }
            }
            else
            {
                Type finalValueType = finalValue.GetType();
                Type fieldType = _proxy.Reflection.GetFieldReturnType(finalValueType, lastSegment, true);

                newValue = ParseValue(value, fieldType);
                _proxy.Reflection.SetInstanceProperty(finalValue, lastSegment, newValue);
            }

            return newValue;
        }

        private System.Type InferTypeFromStringValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                return typeof(string);
            }
            else if(Int32.TryParse(value, out _))
            {
                return typeof(Int32);
            }
            else if (Double.TryParse(value, out _))
            {
                return typeof(Double);
            }
            else if (Boolean.TryParse(value, out _))
            {
                return typeof(Boolean);
            }
            else if (Int64.TryParse(value, out _))
            {
                return typeof(Int64);
            }
            else if (Guid.TryParse(value, out _))
            {
                return typeof(Guid);
            }

            return null;
        }

        private object ParseValue(string value, Type type)
        {
            Type inferedType = InferTypeFromStringValue(value);

            if (type is null && inferedType is null)
            {
                throw new InvalidOperationException("Cannot determine type from value.");
            }

            Type targetType = type ?? inferedType;

            if (inferedType is null && type is null && inferedType != type)
            {
                throw new InvalidOperationException($"Value '{value}' cannot be converted to type '{type.FullName}'.");
            }

            if (targetType == typeof(string))
            {
                return value.Substring(1, value.Length - 2);
            }
            else if (targetType == typeof(Int32))
            {
                return Int32.Parse(value);
            }
            else if (targetType == typeof(Decimal))
            {
                return Decimal.Parse(value);
            }
            else if (targetType == typeof(Boolean))
            {
                return Boolean.Parse(value);
            }
            else if (targetType == typeof(Int64))
            {
                return Int64.Parse(value);
            }
            else if (targetType == typeof(Guid))
            {
                return Guid.Parse(value);
            }

            throw new InvalidOperationException($"Type '{targetType.FullName}' is not supported for parsing.");
        }

    }
}
