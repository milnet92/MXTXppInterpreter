using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Interpreter
{
    public static class XppInterpeterUtil
    {
        public static object CallFunction(Parser.FunctionDeclaration declaration, object[] parameters, XppProxy proxy, XppInterpreterOptions options = null)
        {
            // Add the statement to call the function
            Parser.Program program = new Parser.Program(declaration.Block.Statements, null);

            XppInterpreter interpreter = new XppInterpreter(proxy, options);

            // Set the variables to the context
            RuntimeContext context = new RuntimeContext(proxy, interpreter.Compile(program));

            for (int i = 0; i < declaration.Parameters.Count; i++)
            {
                var param = declaration.Parameters[i];
                context.ScopeHandler.CurrentScope.SetVar(param.Name, parameters[i], proxy.Casting, true, parameters[i].GetType());
            }

            interpreter.Interpret(context.ByteCode, context, true);

            return context.Stack.Pop();
        }

        public static Dictionary<string, string> GetFunctionParameters(Parser.FunctionDeclaration declaration)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            foreach (var param in declaration.Parameters)
            {
                parameters.Add(param.Name, param.DeclarationType.Lexeme);
            }

            return parameters;
        }
    }
}
