using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Parser.Metadata.Providers
{
    class IntrinsicMethodMetadataProvider : ITokenMetadataProvider
    {
        public string IntrinsicMethodName { get; }

        public IntrinsicMethodMetadataProvider(string methodName)
        {
            IntrinsicMethodName = methodName;
        }

        public TokenMetadata GetTokenMetadata(XppProxy proxy)
        {
            StringBuilder builder = new StringBuilder();
            System.Reflection.MethodInfo method = Core.ReflectionHelper.GetMethod(proxy.Intrinsic.GetType(), IntrinsicMethodName);

            builder.Append("(builtin function) ");
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

                builder.Append(parm.Name);

                parmNum++;
            }

            builder.Append(')');

            return new IntrinsicMethodTokenMetadata(builder.ToString());
        }
    }
}
