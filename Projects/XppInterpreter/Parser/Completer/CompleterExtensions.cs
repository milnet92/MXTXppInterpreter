using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Parser.Completer
{
    public static class CompleterExtensions
    {
        public static string GenerateCompleterDocHtml(this MethodInfo method, XppProxy proxy)
        {
            StringBuilder builder = new StringBuilder();

            string returnType = method.ReturnType == typeof(void) ? "void" : method.ReturnType.Name;

            builder.Append($"<span>{returnType}</span> <span>{method.Name}</span>");
            builder.Append("(");

            foreach (var parameter in method.GetParameters())
            {
                if (parameter.Position > 0)
                {
                    builder.Append(", ");
                }

                builder.Append($"{parameter.ParameterType.Name} {parameter.Name}");

                if (parameter.HasDefaultValue)
                {
                    builder.Append($" = {parameter.DefaultValue ?? "null"}");
                }
            }

            builder.Append(")");

            return builder.ToString();
        }
        public static string GenerateCompleterDocHtml(this PropertyInfo property, XppProxy proxy)
        {
            return $"<span>{property.PropertyType.Name}</span> <span>{property.Name}</span>";
        }
        public static string GenerateCompleterDocHtml(this FieldInfo field, XppProxy proxy)
        {
            return $"<span>{field.FieldType.Name}</span> <span>{field.Name}</span>";
        }
    }
}
