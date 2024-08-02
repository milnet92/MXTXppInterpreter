using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Metadata
{
    public class LabelTokenMetadata : TokenMetadata
    {
        public string LabelText { get; }
        public string LabelId { get; }

        public LabelTokenMetadata(string labelId, string labelText)
        {
            LabelId = labelId;
            LabelText = labelText;
        }

        public override string GetDisplayHtml()
        {
            return Span(string.IsNullOrEmpty(LabelText) ? LabelId : LabelText, COLOR_CSS_TEXT);
        }
    }
}
