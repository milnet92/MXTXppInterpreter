using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Completer
{
    class MetadataInterruption : Exception
    {
        public TokenMetadata TokenData { get; }

        public MetadataInterruption(TokenMetadata metadata)
        {
            TokenData = metadata;
        }
    }
}
