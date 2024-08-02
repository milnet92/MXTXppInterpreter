using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Metadata
{
    public class MethodTokenMetadata : TokenMetadata
    {
        private static string[] METHOD_KEYWORDS = new string[] {
            "public", "private", "static", "client", "server", "internal", "abstract", "protected", "abstract", "class", "interface", "edit", "display", 
            "str", "container", "real", "date", "time", "utcdatetime", "anytype", "void", "int", "int64", "guid" };
        
        public string MethodName { get; }
        public string ClassName { get; }
        public string XppSyntax { get; }
        public bool IsStatic { get; }

        public MethodTokenMetadata(string methodName, string className, string xppSyntax, bool isStatic)
        {
            MethodName = methodName;
            ClassName = className;
            XppSyntax = xppSyntax;
            IsStatic = isStatic;
        }

        public override string GetDisplayHtml()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string syntax = XppSyntax.Replace("\n", "");
            string[] methodTokens = syntax.Split(new char[] { ' ', '(', ',' }, StringSplitOptions.RemoveEmptyEntries);

            bool parsingParameters = false;
            bool parsingType = false;
            int parmNum = 0;
            foreach (var token in methodTokens)
            {
                if (!parsingParameters)
                {
                    if (IsKeyword(token))
                    {
                        stringBuilder.Append(Span(token, COLOR_CSS_BLUE));
                        stringBuilder.Append(' ');
                    }
                    else if (!string.Equals(token, MethodName, StringComparison.InvariantCultureIgnoreCase))
                    { 
                        stringBuilder.Append(Span(token, COLOR_CSS_CYAN));
                        stringBuilder.Append(' ');
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(ClassName))
                        {
                            stringBuilder.Append($"{token}(");
                        }
                        else
                        { 
                            stringBuilder.Append($"{Span(ClassName, COLOR_CSS_CYAN)}{GetMethodDelimiter()}{token}(");
                        }
                        parsingParameters = true;
                        parsingType = true;
                    }
                }
                else
                {
                    if (parsingType && parmNum > 0)
                        stringBuilder.Append(", ");

                    stringBuilder.Append(token);

                    if (parsingType)
                    {
                        stringBuilder.Append(' ');
                    }
                    else
                    {
                        parmNum ++;
                    }

                    parsingType = !parsingType;

                }
            }

            return stringBuilder.ToString();
        }

        private bool IsKeyword(string token)
        {
            return METHOD_KEYWORDS.Contains(token, StringComparer.InvariantCultureIgnoreCase);
        }

        private string GetMethodDelimiter()
        {
            return IsStatic ? "::" : ".";
        }
    }
}
