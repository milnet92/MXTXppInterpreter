using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Completer
{
    public class TokenMetadata
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Prefix { get; set; }
        public string DocHtml { get; set; }
    }
}
