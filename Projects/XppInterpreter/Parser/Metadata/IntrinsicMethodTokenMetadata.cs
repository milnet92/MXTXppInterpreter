using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Metadata
{
    public class IntrinsicMethodTokenMetadata : TokenMetadata
    {
        public string SyntaxHtml { get; }
        public IntrinsicMethodTokenMetadata(string syntaxHtml)
        { 
            SyntaxHtml = syntaxHtml;
        }

        public override string GetDisplayHtml()
        {
            return SyntaxHtml;
        }
    }
}
