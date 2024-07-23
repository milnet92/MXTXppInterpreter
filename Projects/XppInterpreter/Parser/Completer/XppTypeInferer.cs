using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser.Completer
{
    class XppTypeInferer : ITypeInferExpressionVisitor
    {
        private System.Type _globalType, _predefinedType;
        private readonly XppProxy _proxy;
        private Token _triggerToken = null;

        private readonly Dictionary<string, Word> _localVariableTypes = new Dictionary<string, Word>(StringComparer.InvariantCultureIgnoreCase);

        public XppTypeInferer(XppProxy proxy)
        {
            _proxy = proxy;

            // initialize types
            _globalType = _proxy.Casting.GetSystemTypeFromTypeName("Global");
            _predefinedType = _proxy.Casting.GetSystemTypeFromTypeName("PredefinedFunctions");
        }

        public System.Type InferType(Expression expression, Token triggerToken)
        {
            _triggerToken = triggerToken;
            return expression.Accept(this);
        }

        public void AddExpressionType(string varName, Word word)
        {
            _localVariableTypes.Add(varName, word);
        }

        private bool IsGlobalFunction(string methodName)
        {
            return Core.ReflectionHelper.TypeHasMethod(_globalType, methodName)
                || Core.ReflectionHelper.TypeHasMethod(_predefinedType, methodName);
        }

        public System.Type VisitBinaryOperation(BinaryOperation binaryOperation)
        {
            var leftType = binaryOperation.LeftOperand.Accept(this);
            var rightType = binaryOperation.RightOperand.Accept(this);

            if (leftType == typeof(decimal) || rightType == typeof(decimal))
            { 
                return typeof(decimal);
            }

            return leftType ?? rightType;
        }

        public System.Type VisitConstant(Constant constant)
        {
            if (constant.Token is BaseType baseType)
            {
                return baseType.Value?.GetType();
            }
            else if (constant.Token == Word.True || constant.Token == Word.False)
            {
                return typeof(bool);
            }

            return null;
        }

        public System.Type VisitConstructor(Constructor constructor)
        {
            return _proxy.Casting.GetSystemTypeFromTypeName(constructor.ClassName);
        }

        public System.Type VisitContainerInitialisation(ContainerInitialisation containerInitialisation)
        {
            return typeof(object[]);
        }

        public System.Type VisitFunctionCall(FunctionCall functionCall)
        {
            if (functionCall.Caller != null)
            {
                System.Type callerType = functionCall.Caller.Accept(this);

                if (callerType != null)
                {
                    return Core.ReflectionHelper.GetMethod(callerType, functionCall.Name)?.ReturnType;
                }
            }
            else if (IsGlobalFunction(functionCall.Name))
            {
                var globalMethod = 
                    Core.ReflectionHelper.GetMethod(_globalType, functionCall.Name) 
                    ?? Core.ReflectionHelper.GetMethod(_predefinedType, functionCall.Name);

                if (globalMethod != null)
                {
                    return globalMethod.ReturnType;
                }
            }

            return null;
        }

        public System.Type VisitTernary(Ternary ternary)
        {
            return ternary.Left.Accept(this) ?? ternary.Right.Accept(this);
        }

        public System.Type VisitUnaryOperation(UnaryOperation unaryOperation)
        {
            switch (unaryOperation.Token.TokenType)
            {
                case TType.Negation:
                    return typeof(bool);
                case TType.Minus:
                    return unaryOperation.Expression.Accept(this);
                default:
                    return null;
            }
        }

        public System.Type VisitVariable(Variable variable)
        {
            if (variable.Caller != null)
            {
                System.Type callerType = variable.Caller.Accept(this);

                if (Core.ReflectionHelper.TypeHasField(callerType, variable.Name))
                {
                    return Core.ReflectionHelper.GetField(callerType, variable.Name).FieldType;
                }
                else if (Core.ReflectionHelper.TypeHasProperty(callerType, variable.Name))
                {
                    return Core.ReflectionHelper.GetProperty(callerType, variable.Name).PropertyType;
                }
            }
            else
            {
                if (_triggerToken.TokenType == TType.StaticDoubleDot)
                {
                    return GetTypeFromWord(variable.Token as Word);
                }
                // Infer from variables
                else if (_localVariableTypes.ContainsKey(variable.Name))
                {
                    return GetTypeFromWord(_localVariableTypes[variable.Name]);
                }
            }

            return null;
        }

        private System.Type GetTypeFromWord(Word word)
        {
            if (word is null) return null;
            return _proxy.Casting.GetSystemTypeFromTypeName(word.Lexeme);
        }
    }
}
