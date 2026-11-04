using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace XppInterpreter.Parser.Metadata
{
    /// <summary>
    /// The hint of a .NET method: all its overloads, one per line.
    /// </summary>
    public class ClrMethodTokenMetadata : TokenMetadata
    {
        public string[] Signatures { get; }

        public ClrMethodTokenMetadata(string[] signatures)
        {
            Signatures = signatures;
        }

        public override string GetDisplayHtml()
        {
            return string.Join("<br/>", Signatures.Select(WebUtility.HtmlEncode));
        }

        /// <summary>
        /// All the overloads, with the parameter being written in bold.
        /// </summary>
        public string GetDisplayHtml(int parameterPosition)
        {
            return string.Join("<br/>", Signatures.Select(s => Highlight(s, parameterPosition)));
        }

        private static string Highlight(string signature, int parameterPosition)
        {
            int open = signature.IndexOf('(');
            string parameters = signature.Substring(open + 1, signature.Length - open - 2);

            var parts = SplitParameters(parameters).Select(WebUtility.HtmlEncode).ToArray();

            if (parameterPosition < parts.Length)
            {
                parts[parameterPosition] = $"<b>{parts[parameterPosition]}</b>";
            }

            return $"{WebUtility.HtmlEncode(signature.Substring(0, open))}({string.Join(", ", parts)})";
        }

        // The comma of a generic type or an array does not separate parameters
        private static IEnumerable<string> SplitParameters(string parameters)
        {
            var current = new StringBuilder();
            int depth = 0;

            foreach (char c in parameters)
            {
                if (c == '<' || c == '[') depth++;
                if (c == '>' || c == ']') depth--;

                if (c == ',' && depth == 0)
                {
                    yield return current.ToString().Trim();
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                yield return current.ToString().Trim();
            }
        }
    }
}
