﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Parser.Metadata.Providers;

namespace XppInterpreter.Parser.Metadata
{
    public class TokenMetadataProviderHelper
    {
        public static TokenMetadata GetLocalVariableMetadata(string varName, XppProxy proxy, ParseContext context)
        {
            return new LocalVariableMetadataProvider(varName, context).GetTokenMetadata(proxy);
        }

        public static TokenMetadata GetLabelMetadata(string labelId, XppProxy proxy)
        {
            return new LabelMetadataProvider(labelId).GetTokenMetadata(proxy);
        }

        public static TokenMetadata GetMethodMetadata(
            Type callerType,
            string methodName,
            bool isIntrinsic,
            bool isStatic,
            bool isConstructor,
            XppProxy proxy,
            ParseContext context)
        {
            ITokenMetadataProvider provider;

            if (isIntrinsic)
            {
                provider = new IntrinsicMethodMetadataProvider(methodName);
            }
            else
            {
                provider = new MethodMetadataProvider(methodName, context, isStatic, isConstructor, callerType);
            }

            return provider.GetTokenMetadata(proxy);
        }

        public static TokenMetadata GetMetadataForMethodParameters(
            Type callerType, 
            string methodName,
            bool isIntrinsic,
            bool isStatic,
            bool isConstructor,
            int parameterPosition, 
            XppProxy proxy,
            ParseContext context)
        {
            if (isIntrinsic)
            {
                return new IntrinsicMethodTokenMetadata(GenerateIntrinsicSignatureHtml(methodName, proxy, parameterPosition));
            }

            string syntax = new Providers.MethodMetadataProvider(methodName, context, isStatic, isConstructor, callerType)
                .GetTokenMetadata(proxy)
                .GetDisplayHtml();

            StringBuilder builder = new StringBuilder();
            int parsingParamCount = -1;
            syntax = System.Text.RegularExpressions.Regex.Replace(syntax, "client[\\s]|server[\\s]", "");

            foreach (var c in syntax)
            {
                if (c == ',' || c == '(' || c == ')')
                {
                    parsingParamCount ++;

                    if (parameterPosition == parsingParamCount)
                    {
                        builder.Append("<b>");
                    }

                    if (parameterPosition == (parsingParamCount - 1))
                    {
                        builder.Append("</b>");
                    }
                }

                builder.Append(c);
            }

            return new IntrinsicMethodTokenMetadata(builder.ToString());
        }

        public static string GetConstructorSyntax(ConstructorInfo constructorInfo)
        {
            return "new" + "(" + GetMethodBaseParameterSytnax(constructorInfo) + ")";
        }

        public static string GetMethodSyntax(MethodInfo methodInfo)
        {
            return methodInfo.Name + "(" + GetMethodBaseParameterSytnax(methodInfo) + ")";
        }

        private static string GetMethodBaseParameterSytnax(MethodBase methodBase)
        {
            StringBuilder builder = new StringBuilder();

            if (methodBase is MethodInfo info)
                builder.Append(info.ReturnType?.ToString() ?? string.Empty);

            int parameterNum = 0;
            foreach (var parameter in methodBase.GetParameters())
            {
                if (parameterNum > 0)
                    builder.Append(", ");

                if (parameter.HasDefaultValue)
                    builder.Append("[");

                builder.Append(parameter.ParameterType.Name);
                builder.Append(" ");
                builder.Append(parameter.Name);

                if (parameter.HasDefaultValue)
                {
                    builder.Append(" = ");
                    builder.Append(parameter.DefaultValue?.ToString() ?? "null");
                    builder.Append("]");
                }

                parameterNum ++;
            }

            return builder.ToString();
        }

        private static string GenerateIntrinsicSignatureHtml(string methodName, XppProxy proxy, int parameterPosition)
        {
            StringBuilder builder = new StringBuilder();
            System.Reflection.MethodInfo method = Core.ReflectionHelper.GetMethod(proxy.Intrinsic.GetType(), methodName);

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

                if (parameterPosition == parmNum)
                {
                    builder.Append("<b>");
                }

                builder.Append(parm.Name);

                if (parameterPosition == parmNum)
                {
                    builder.Append("</b>");
                }

                parmNum ++;
            }

            builder.Append(')');

            return builder.ToString();
        }
    }
}
