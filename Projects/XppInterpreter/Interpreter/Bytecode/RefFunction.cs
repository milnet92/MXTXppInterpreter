using System.Collections.Generic;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class RefFunction
    {
        public FunctionDeclaration Declaration;
        public List<IInstruction> Instructions = new List<IInstruction>();

        public RefFunction(FunctionDeclaration declaration)
        {
            Declaration = declaration;
        }

        /// <summary>
        /// Declares the parameters that got an argument in the scope of a call. The others get their default value from the instructions.
        /// </summary>
        internal void BindArguments(Core.Scope scope, object[] arguments, Proxy.XppProxy proxy)
        {
            for (int numParam = 0; numParam < arguments.Length; numParam++)
            {
                var funcParameter = Declaration.Parameters[numParam];
                System.Type declarationType = funcParameter.DeclarationClrType;

                System.Diagnostics.Debug.Assert(declarationType != null); // We MUST have a declaration type

                object argument = arguments[numParam];

                if (argument is null && proxy.IsCommonType(declarationType))
                {
                    argument = proxy.Casting.GetDefaultValueForType(funcParameter.DeclarationType.Lexeme);
                }

                scope.SetVar(funcParameter.Name, argument, proxy, true, declarationType);
            }
        }
    }
}
