using System;
using System.Collections.Generic;
using XppInterpreter.Lexer;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class Arithmetic : IInstruction
    {
        public string OperationCode => _arithmeticOpCodes[TokenType];

        public TType TokenType { get; private set; }

        public Arithmetic(TType tokenType)
        {
            if (!_arithmeticOpCodes.ContainsKey(tokenType))
            {
                throw new ArgumentException($"{tokenType} is invalid");
            }

            TokenType = tokenType;

        }

        public void Execute(RuntimeContext context)
        {
            object right = context.Stack.Pop();
            object left = context.Stack.Pop();

            object result = null;

            switch (TokenType)
            {
                case TType.Plus:
                case TType.PlusAssignment:
                case TType.Increment:
                    result = context.Proxy.Binary.Add(left, right);
                    break;

                case TType.Minus:
                case TType.MinusAssignment:
                case TType.Decrement:
                    result = context.Proxy.Binary.Substract(left, right);
                    break;

                case TType.Star:
                    result = context.Proxy.Binary.Multiply(left, right);
                    break;

                case TType.Division:
                    result = context.Proxy.Binary.Divide(left, right);
                    break;

                case TType.IntegerDivision:
                    result = context.Proxy.Binary.Divide(left, right);
                    break;

                case TType.Mod:
                    result = context.Proxy.Binary.Mod(left, right);
                    break;

                case TType.Equal:
                    result = context.Proxy.Binary.AreEqual(left, right);
                    break;

                case TType.NotEqual:
                    result = !context.Proxy.Binary.AreEqual(left, right);
                    break;

                case TType.Greater:
                    result = context.Proxy.Binary.Greater(left, right);
                    break;

                case TType.GreaterOrEqual:
                    result = context.Proxy.Binary.Greater(left, right) || context.Proxy.Binary.AreEqual(left, right);
                    break;

                case TType.Smaller:
                    result = !context.Proxy.Binary.Greater(left, right) && !context.Proxy.Binary.AreEqual(left, right);
                    break;

                case TType.SmallerOrEqual:
                    result = !context.Proxy.Binary.Greater(left, right);
                    break;
            }

            context.Stack.Push(result);
        }

        public static bool IsArithmetic(TType tokenType)
        {
            return _arithmeticOpCodes.ContainsKey(tokenType);
        }

        private readonly static Dictionary<TType, string> _arithmeticOpCodes = new Dictionary<TType, string>()
        {
            { TType.Plus, "ADD" },
            { TType.PlusAssignment, "ADD" },
            { TType.Increment, "ADD" },
            { TType.Minus, "SUB" },
            { TType.MinusAssignment, "SUB" },
            { TType.Decrement, "SUB" },
            { TType.Star, "MUL" },
            { TType.Division, "DIV" },
            { TType.IntegerDivision, "INTDIV" },
            { TType.Mod, "MOD" },
            { TType.Equal, "EQ" },
            { TType.NotEqual, "NE" },
            { TType.Greater, "GR" },
            { TType.GreaterOrEqual, "GE" },
            { TType.Smaller, "SM" },
            { TType.SmallerOrEqual, "SME" },
        };
    }
}
