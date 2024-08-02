using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Parser.Metadata.Providers
{
    public class LabelMetadataProvider : ITokenMetadataProvider
    {
        public string LabelId { get; }
        public LabelMetadataProvider(string labelId)
        {
            LabelId = labelId;
        }

        public TokenMetadata GetTokenMetadata(XppProxy proxy)
        {
            return new LabelTokenMetadata(LabelId, proxy.Reflection.LabelIdToValue(LabelId, null));
        }
    }
}
