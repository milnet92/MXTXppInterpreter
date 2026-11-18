using System;
using XppInterpreter.Interpreter.Bytecode;

namespace XppInterpreter.Interpreter.Query
{
    public class QueryGenerationHelper
    {
        private readonly RuntimeContext _context;
        public QueryGenerationHelper(RuntimeContext context)
        {
            _context = context;
        }

        private object Interpret(ByteCode byteCode)
        {
            XppInterpreter interpreter = new XppInterpreter(_context.Proxy);
            interpreter.Interpret(byteCode, _context, false);

            return _context.Stack.Pop();
        }

        public object GetDefaultValueForType(Type type)
        {
            return _context.Proxy.Casting.GetDefaultValueForSystemType(type);
        }

        public object ComputeVariable(Parser.Variable variable) => ComputeExpression(variable);

        public object ComputeFunctionCall(Parser.FunctionCall functionCall) => ComputeExpression(functionCall);

        public object ComputeConstant(Parser.Constant constant) => ComputeExpression(constant);


        public object ComputeExpression(Parser.Expression expression)
        {
            // Most values of a where clause are a literal or a variable, and a select inside a loop asks for them every
            // time: these two are what their bytecode would push, without compiling and running a program for it
            if (expression.GetType() == typeof(Parser.Constant))
            {
                var token = expression.Token;

                return token is Lexer.BaseType literal ? literal.Value
                    : token.TokenType == Lexer.TType.True ? true
                    : token.TokenType == Lexer.TType.False ? (object)false
                    : null;
            }

            if (expression.GetType() == typeof(Parser.Variable) && expression is Parser.Variable variable && variable.Caller is null && !variable.IsSystemVariable)
            {
                return _context.ScopeHandler.GetVar(variable.Name);
            }

            // Compile
            var generator = new Bytecode.ByteCodeGenerator(declaredFunctions: _context.ByteCode.DeclaredFunctions);
            expression.Accept(generator);
            ByteCode bytecode = generator.GetProgram();

            return Interpret(bytecode);
        }

    }
}
