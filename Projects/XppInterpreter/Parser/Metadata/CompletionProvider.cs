using System;
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

            if (interruption.Namespace != null)
            {
                completions.Union(GetNamespaceCompletions(interruption.Namespace));
                return completions;
            }

            System.Type inferedType = interruption.InferedType;

            if (inferedType is null) return completions;

            if (!_proxy.Reflection.IsXppType(inferedType))
            {
                if (purpose == AutoCompletionPurpose.InstanceMembers || purpose == AutoCompletionPurpose.StaticMembers)
                {
                    completions.Union(GetClrCompletions(inferedType, purpose == AutoCompletionPurpose.StaticMembers));
                }

                return completions;
            }

            if (purpose == AutoCompletionPurpose.InstanceMembers ||
                purpose == AutoCompletionPurpose.StaticMembers)
            { 
                bool @static = purpose == AutoCompletionPurpose.StaticMembers;

                if (_proxy.Reflection.IsCommonType(inferedType))
                {
                    completions.Union(nativeProvider.GetTableMethodCompletions(inferedType.Name, @static));

                    if (!@static)
                    {
                        completions.Union(nativeProvider.GetTableFieldsCompletions(inferedType.Name));
                    }
                }
                else
                {
                    if (@static && _proxy.Reflection.IsEnum(inferedType.Name))
                    {
                        completions.Union(nativeProvider.GetEnumCompletions(inferedType.Name));
                    }
                    else
                    {
                        completions.Union(nativeProvider.GetClassMethodCompletions(inferedType.Name, @static));
                        completions.Union(nativeProvider.GetClassFieldCompletions(inferedType.Name, @static));
                    }
                }
            }
            else if (purpose == AutoCompletionPurpose.TableIndexes)
            {
                if (_proxy.Reflection.IsCommonType(inferedType))
                { 
                    completions.Union(nativeProvider.GetIndexCompletions(inferedType.Name));
                }
            }

            return completions;
        }

        private CompletionCollection GetNamespaceCompletions(string @namespace)
        {
            CompletionCollection completions = new CompletionCollection();

            foreach (var member in _proxy.ClrTypes.GetNamespaceMembers(@namespace).OrderBy(m => m))
            {
                bool isNamespace = member.EndsWith(".");
                string name = isNamespace ? member.TrimEnd('.') : member;

                completions.Add(new Completion(name, name, isNamespace ? CompletionEntryType.Namespace : CompletionEntryType.Class)
                {
                    DocHtml = isNamespace ? $"namespace {@namespace}.{name}" : $"{@namespace}.{name}"
                });
            }

            return completions;
        }

        private CompletionCollection GetClrCompletions(System.Type type, bool @static)
        {
            CompletionCollection completions = new CompletionCollection();

            if (type.IsEnum && @static)
            {
                foreach (var member in System.Enum.GetNames(type))
                {
                    completions.Add(new Completion(member, member, CompletionEntryType.EnumValue));
                }

                return completions;
            }

            foreach (var method in Core.Clr.ClrBinder.GetPublicMethods(type, @static))
            {
                completions.Add(new Completion(method.Name, method.Name, @static ? CompletionEntryType.StaticMethod : CompletionEntryType.Method)
                {
                    DocHtml = string.Join("<br/>", Core.Clr.ClrBinder.GetMethodSignatures(type, method.Name, @static).Select(System.Net.WebUtility.HtmlEncode))
                });
            }

            foreach (var member in Core.Clr.ClrBinder.GetPublicMembers(type, @static))
            {
                completions.Add(new Completion(member.Name, member.Name, CompletionEntryType.ClassProperty)
                {
                    DocHtml = System.Net.WebUtility.HtmlEncode($"{Core.Clr.ClrBinder.FriendlyName(Core.Clr.ClrBinder.GetMemberType(type, member.Name, @static))} {member.Name}")
                });
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
