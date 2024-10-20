using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Parser.Metadata.Providers
{
    public class MethodMetadataProvider : ITokenMetadataProvider
    {
        public string MethodName { get; }
        public ParseContext ParseContext { get; }
        public string Namespace { get; }
        public Type CallerType { get; }
        public bool IsStatic { get; }
        public bool IsConstructor { get; }

        public MethodMetadataProvider(string methodName, string nameSpace, ParseContext context, bool isStatic, bool isConstructor, Type callerType = null)
        {
            MethodName = methodName;
            ParseContext = context;
            CallerType = callerType;
            IsStatic = isStatic;
            IsConstructor = isConstructor;
            Namespace = nameSpace;
        }

        public TokenMetadata GetTokenMetadata(XppProxy proxy)
        {
            if (IsDeclaredFunction())
            {
                return GetDeclaredFunctionTokenMetadata();
            }
            else
            {
                return GetNativeTokenMetadata(proxy);
            }
        }

        private TokenMetadata GetDeclaredFunctionTokenMetadata()
        {
            FunctionDeclaration declaration = ParseContext.CurrentScope.FunctionDeclarations.FirstOrDefault(f => string.Equals(f.Name, MethodName, StringComparison.InvariantCultureIgnoreCase));
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(((Lexer.Word)declaration.Type).Lexeme);
            stringBuilder.Append(' ');
            stringBuilder.Append(declaration.Name);
            stringBuilder.Append('(');
            int numParameter = 0;
            foreach (var parameter in declaration.Parameters)
            {
                if (numParameter > 0)
                {
                    stringBuilder.Append(", ");
                }

                stringBuilder.Append($"{parameter.DeclarationType} {parameter.Name}");
                numParameter++;
            }
            stringBuilder.Append(')');

            return new IntrinsicMethodTokenMetadata(stringBuilder.ToString());
        }

        private TokenMetadata GetNativeTokenMetadata(XppProxy proxy)
        {
            string methodName = IsConstructor ? "new" : MethodName;
            string className = IsConstructor ? MethodName : CallerType?.Name ?? "";

            var methodSyntax = proxy.Reflection.GetMethodSyntax(className, methodName, Namespace);

            return new MethodTokenMetadata(methodName, className, methodSyntax, IsStatic);
        }

        private bool IsDeclaredFunction()
        {
            return CallerType is null && 
                !IsConstructor && 
                ParseContext.CurrentScope.FunctionDeclarations.Any(f => string.Equals(f.Name, MethodName, StringComparison.InvariantCultureIgnoreCase));    
        }
    }
}
