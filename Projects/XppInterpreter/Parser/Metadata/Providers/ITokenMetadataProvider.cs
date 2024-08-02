using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Parser.Metadata
{
    public interface ITokenMetadataProvider
    {
        TokenMetadata GetTokenMetadata(XppProxy proxy);
    }
}
