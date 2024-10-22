﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser.Metadata
{
    public class CompletionProvider
    {
        private readonly XppProxy _proxy;

        public CompletionProvider(XppProxy proxy)
        {
            _proxy = proxy;
        }

        public CompletionCollection GetCompletions(ICompletionProvider nativeProvider, string sourceCode, int row, int column, AutoCompletionPurpose purpose)
        {
            XppLexer lexer = new XppLexer(sourceCode);
            XppParser parser = new XppParser(lexer, _proxy);

            AutoCompletionTypeInterruption interruption = parser.ParseForAutoCompletion(row, column, purpose);

            if (interruption is null) return new CompletionCollection();

            CompletionCollection completions = new CompletionCollection();

            if (interruption.InferedType != null)
            {
                System.Type inferedType = interruption.InferedType;

                if (purpose == AutoCompletionPurpose.InstanceMembers ||
                    purpose == AutoCompletionPurpose.StaticMembers)
                {
                    bool @static = purpose == AutoCompletionPurpose.StaticMembers;

                    if (_proxy.Reflection.IsCommonType(inferedType))
                    {
                        completions.AddRange(nativeProvider.GetTableMethodCompletions(inferedType.Name, @static));

                        if (!@static)
                        {
                            completions.AddRange(nativeProvider.GetTableFieldsCompletions(inferedType.Name));
                        }
                    }
                    else
                    {
                        if (@static && _proxy.Reflection.IsEnum(inferedType.Name))
                        {
                            completions.AddRange(nativeProvider.GetEnumCompletions(inferedType.Name));
                        }
                        else
                        {
                            completions.AddRange(nativeProvider.GetClassMethodCompletions(inferedType.Namespace, inferedType.Name, @static));
                            completions.AddRange(nativeProvider.GetClassFieldCompletions(inferedType.Namespace, inferedType.Name, @static));
                        }
                    }
                }
                else if (purpose == AutoCompletionPurpose.TableIndexes)
                {
                    if (_proxy.Reflection.IsCommonType(inferedType))
                    {
                        completions.AddRange(nativeProvider.GetIndexCompletions(inferedType.Name));
                    }
                }
            }
            else if (!string.IsNullOrEmpty(interruption.Namespace) && purpose == AutoCompletionPurpose.InstanceMembers)
            {
                completions.AddRange(nativeProvider.GetNamespaceCompletions(interruption.Namespace));
            }

            return completions;
        }

        private IEnumerable<System.Reflection.MethodInfo> GetMethods(System.Type type, bool @static)
        {
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | (@static ? System.Reflection.BindingFlags.Static : System.Reflection.BindingFlags.Instance);

            return type.GetMethods(flags)
                .Where(m => 
                !m.IsSpecialName 
                && (!m.IsHideBySig || (m.Attributes & System.Reflection.MethodAttributes.VtableLayoutMask) == System.Reflection.MethodAttributes.NewSlot)
                && !m.Name.StartsWith("_") 
                && !m.Name.StartsWith("`") 
                && !m.Name.StartsWith("$"));
        }

        private IEnumerable<System.Reflection.FieldInfo> GetFields(System.Type type, bool @static)
        {
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | (@static ? System.Reflection.BindingFlags.Static : System.Reflection.BindingFlags.Instance);

            return type.GetFields(flags).Where(m => !m.IsSpecialName && !m.Name.StartsWith("_") && !m.Name.StartsWith("`") && !m.Name.StartsWith("$"));
        }

        private IEnumerable<System.Reflection.PropertyInfo> GetProperties(System.Type type, bool @static)
        {
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | (@static ? System.Reflection.BindingFlags.Static : System.Reflection.BindingFlags.Instance);

            return type.GetProperties(flags).Where(m => !m.IsSpecialName && !m.Name.StartsWith("_") && !m.Name.StartsWith("`") && !m.Name.StartsWith("$"));
        }
    }
}
