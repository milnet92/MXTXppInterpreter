using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Core;
using XppInterpreter.Parser.Metadata;

namespace XppInterpreter.Parser
{
    internal static class EventHandlerHelper
    {
        internal static void ValidateEventHandler(XppParser parser, ParseContext context, Type delegateType, EventHandler eventHandler)
        {
            var function = context.CurrentScope.FunctionDeclarations.FirstOrDefault(f => string.Equals(f.Name, eventHandler.FunctionName, StringComparison.InvariantCultureIgnoreCase));
            var delegateParams = delegateType.GetMethod("Invoke").GetParameters();

            // We already validated the function on the event handler itself
            if (function is null) return;

            // Validate parameters
            if (function.Parameters.Count != delegateParams.Length)
            {
                parser.HandleParseError("Number of parameters does not match for the delegate.", stop: false);
            }
            else
            {
                for (int i = 0; i < function.Parameters.Count; i++)
                {
                    if (function.Parameters[i].DeclarationClrType?.FullName != delegateParams[i].ParameterType.FullName)
                    {
                        parser.HandleParseError("Parameter types does not match with the delegate parameter types.", stop: false);
                    }
                }
            }

        }

        internal static bool IsEventHandler(Expression expression, ParseContext context, XppTypeInferer typeInferer)
        {
            return expression is Variable && !(expression is FunctionCall) &&
                ReflectionHelper.IsDelegateType(typeInferer.InferType(expression, context));
        }
    }
}
