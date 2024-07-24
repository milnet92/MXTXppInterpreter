using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser.Completer
{
    public class CompletionProvider
    {
        private readonly XppProxy _proxy;

        public CompletionProvider(XppProxy proxy)
        {
            _proxy = proxy;
        }

        public CompletionCollection GetCompletions(string sourceCode, int row, int column, bool isStatic)
        {
            XppLexer lexer = new XppLexer(sourceCode);
            XppParser parser = new XppParser(lexer, _proxy);

            System.Type inferedType = parser.ParseForAutoCompletion(row, column);

            CompletionCollection completions = new CompletionCollection();

            if (isStatic)
            {
                AddStaticCompletions(inferedType, completions);
            }
            else
            {
                AddInstanceCompletions(inferedType, completions);
            }

            return completions;
        }

        private void AddStaticCompletions(System.Type type, CompletionCollection completions)
        {
            if (type is null) return;

            if (_proxy.Reflection.IsEnum(type.Name))
            {
                foreach (var enumSymbol in _proxy.Reflection.GetAllEnumValues(type.Name))
                {
                    completions.Add(new Completion(enumSymbol, enumSymbol, CompletionEntryType.EnumValue));
                }
            }
            else
            { 
                AddCompletions(type, completions, true);
            }
        }

        private void AddInstanceCompletions(System.Type type, CompletionCollection completions)
        {
            if (type is null) return;

            AddCompletions(type, completions, false);
        }

        private IEnumerable<System.Reflection.MethodInfo> GetMethods(System.Type type, bool @static)
        {
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | (@static ? System.Reflection.BindingFlags.Static : System.Reflection.BindingFlags.Instance);

            return type.GetMethods(flags).Where(m => !m.IsSpecialName && !m.IsHideBySig && !m.Name.StartsWith("_") && !m.Name.StartsWith("`") && !m.Name.StartsWith("$"));
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

        private void AddCompletions(System.Type type, CompletionCollection completions, bool @static)
        {
            if (type is null) return;

            // Add public methods
            foreach (var methods in GetMethods(type, @static).GroupBy(m => m.Name))
            {
                StringBuilder docHtmlBuilder = new StringBuilder();

                foreach (var method in methods)
                {
                    docHtmlBuilder.AppendLine(method.GenerateCompleterDocHtml(_proxy));
                }

                completions.Add(new Completion(methods.Key, methods.Key, CompletionEntryType.Method)
                {
                    DocHtml = docHtmlBuilder.ToString()
                });
            }

            bool isCommon = _proxy.Reflection.IsCommonType(type);

            // Add fields
            foreach (var fieldGroup in GetFields(type, @static).GroupBy(m => m.Name))
            {
                StringBuilder docHtmlBuilder = new StringBuilder();

                foreach (var field in fieldGroup)
                {
                    docHtmlBuilder.AppendLine(field.GenerateCompleterDocHtml(_proxy));
                }

                completions.Add(new Completion(fieldGroup.Key, fieldGroup.Key, isCommon ? CompletionEntryType.TableField : CompletionEntryType.ClassProperty)
                {
                    DocHtml = docHtmlBuilder.ToString()
                });
            }

            // Add properties
            foreach (var propertyGroup in GetProperties(type, @static).GroupBy(m => m.Name))
            {
                StringBuilder docHtmlBuilder = new StringBuilder();

                foreach (var property in propertyGroup)
                {
                    docHtmlBuilder.AppendLine(property.GenerateCompleterDocHtml(_proxy));
                }

                completions.Add(new Completion(propertyGroup.Key, propertyGroup.Key, isCommon ? CompletionEntryType.TableField : CompletionEntryType.ClassProperty)
                {
                    DocHtml = docHtmlBuilder.ToString()
                });
            }
        }
    }
}
