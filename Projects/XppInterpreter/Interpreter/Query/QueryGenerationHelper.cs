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
            // Compile
            var generator = new Bytecode.ByteCodeGenerator(declaredFunctions: _context.ByteCode.DeclaredFunctions);
            expression.Accept(generator);
            ByteCode bytecode = generator.GetProgram();

            return Interpret(bytecode);
        }

    }
}
