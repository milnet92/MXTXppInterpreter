using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Metadata
{
    public abstract class TokenMetadata
    {
        public const string COLOR_CSS_BLUE = "blue";
        public const string COLOR_CSS_CYAN = "#08A4A7";
        public const string COLOR_CSS_TEXT = "#C45221";

        public abstract string GetDisplayHtml();
        internal string Span(string text, string hexColor)
        {
            return $"<span style=\"color:{hexColor}\">{text}</span>";
        }
    }
}
