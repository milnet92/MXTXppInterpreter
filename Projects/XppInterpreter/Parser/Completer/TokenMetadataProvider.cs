using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Parser.Completer
{
    public class TokenMetadataProvider
    {
        public static TokenMetadata GetMetadataForMethodParameters(System.Type callerType, string methodName, int parameterPosition, XppProxy proxy, bool isIntrinsic)
        {
            if (isIntrinsic)
            {
                return new TokenMetadata()
                {
                    DocHtml = GenerateIntrinsicSignatureHtml(methodName, proxy, parameterPosition)
                };
            }

            string syntax = proxy.Reflection.GetMethodSyntax(callerType?.Name ?? "", methodName);
            StringBuilder builder = new StringBuilder();
            int parsingParamCount = -1;
            syntax = System.Text.RegularExpressions.Regex.Replace(syntax, "\n[\\s]*", "");
            syntax = System.Text.RegularExpressions.Regex.Replace(syntax, "client[\\s]|server[\\s]", "");

            foreach (var c in syntax)
            {
                if (c == ',' || c == '(' || c == ')')
                {
                    parsingParamCount ++;

                    if (parameterPosition == parsingParamCount)
                    {
                        builder.Append("<b>");
                    }

                    if (parameterPosition == (parsingParamCount - 1))
                    {
                        builder.Append("</b>");
                    }
                }

                builder.Append(c);
            }

            return new TokenMetadata()
            {
                DocHtml = builder.ToString()
            };
        }

        private static string GenerateIntrinsicSignatureHtml(string methodName, XppProxy proxy, int parameterPosition)
        {
            StringBuilder builder = new StringBuilder();
            System.Reflection.MethodInfo method = Core.ReflectionHelper.GetMethod(proxy.Intrinsic.GetType(), methodName);

            builder.Append(method.ReturnType.Name);
            builder.Append(' ');
            builder.Append(method.Name);
            builder.Append('(');

            int parmNum = 0;
            foreach (var parm in method.GetParameters())
            {
                if (parmNum > 0)
                {
                    builder.Append(", ");
                }

                if (parameterPosition == parmNum)
                {
                    builder.Append("<b>");
                }

                builder.Append(parm.Name);

                if (parameterPosition == parmNum)
                {
                    builder.Append("</b>");
                }

                parmNum ++;
            }

            builder.Append(')');

            return builder.ToString();
        }
    }
}
