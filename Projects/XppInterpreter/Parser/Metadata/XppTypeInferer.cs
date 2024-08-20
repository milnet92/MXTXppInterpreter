using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser.Metadata
{
    class XppTypeInferer : ITypeInferExpressionVisitor
    {
        private System.Type _globalType, _predefinedType, _customPredefinedType;
        private readonly XppProxy _proxy;
        private bool _calledStatically;
        private ParseContext _context;

        public XppTypeInferer(XppProxy proxy)
        {
            _proxy = proxy;

            // initialize types
            _globalType = _proxy.Casting.GetSystemTypeFromTypeName("Global");
            _predefinedType = _proxy.Casting.GetSystemTypeFromTypeName("PredefinedFunctions");
            _customPredefinedType = _proxy.Intrinsic.GetCustomPredefinedFunctionProvider();
        }

        public System.Type InferType(Expression expression, ParseContext context)
        {
            bool calledStatic = false;

            if (expression is Variable variable)
            {
                calledStatic = variable.StaticCall;
            }

            return InferType(expression, calledStatic, _context);
        }

        public System.Type InferType(Expression expression, bool calledStatic, ParseContext context)
        {
            if (expression.ReturnType != null)
            {
                return expression.ReturnType;
            }

            _calledStatically = calledStatic;
            _context = context;

            var inferedType = expression.Accept(this);
            expression.SetReturnType(inferedType);

            return inferedType;
        }

        private bool IsGlobalFunction(string methodName)
        {
            return Core.ReflectionHelper.TypeHasMethod(_globalType, methodName)
                || Core.ReflectionHelper.TypeHasMethod(_predefinedType, methodName);
        }

        private bool IsCustomPredefinedFunction(string methodName)
        {
            if (_customPredefinedType is null) return false;

            return Core.ReflectionHelper.TypeHasMethod(_customPredefinedType, methodName);
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
                bool staticSave = _calledStatically;
                _calledStatically = functionCall.StaticCall;

                System.Type callerType = functionCall.Caller.Accept(this);

                _calledStatically = staticSave;

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
            else if (IsCustomPredefinedFunction(functionCall.Name))
            {
                return Core.ReflectionHelper.GetMethod(_customPredefinedType, functionCall.Name)?.ReturnType;
            }
            else
            {
                return GetTypeFromUserFunctionDeclaration(functionCall.Name);
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

        public bool IsKnownType(string typeName)
        {
            return _proxy.Casting.GetSystemTypeFromTypeName(typeName) != null;
        }

        public System.Type VisitVariable(Variable variable)
        {
            if (variable.Caller != null)
            {
                bool staticSave = _calledStatically;
                _calledStatically = variable.StaticCall;

                System.Type callerType = variable.Caller.Accept(this);

                _calledStatically = staticSave;

                if (callerType is null) return null;

                if (callerType.IsEnum)
                {
                    return _proxy.Reflection.GetEnumValue(callerType.Name, variable.Name).GetType();
                }
                else if (Core.ReflectionHelper.TypeHasField(callerType, variable.Name))
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
                if (_calledStatically)
                {
                    return GetTypeFromWord(variable.Token as Word);
                }
                // Infer from variables
                else
                {
                    var declaration = _context.CurrentScope.FindVariableDeclaration(variable.Name);

                    if (declaration != null)
                    {
                        if (declaration.Type.TokenType == TType.Var)
                        {
                            if (declaration.Initialization is null) return null;
                            return InferType(declaration.Initialization, false, _context);
                        }

                        return GetTypeFromWord((Word)declaration.Type);

                    }
                }
            }

            return null;
        }

        private System.Type GetTypeFromWord(Word word)
        {
            if (word is null) return null;
            return _proxy.Casting.GetSystemTypeFromTypeName(word.Lexeme);
        }

        public System.Type GetTypeFromUserFunctionDeclaration(string functionName, ParseContext context = null)
        {
            var declarationReference = (context ?? _context).CurrentScope.FindFunctionDeclarationReference(functionName);

            if (declarationReference != null)
            {
                return _proxy.Casting.GetSystemTypeFromTypeName(declarationReference.ReturnType.Lexeme);
            }

            return null;
        }

        public System.Type VisitIs(Is @is)
        {
            return _proxy.Casting.GetSystemTypeFromTypeName(@is.TypeName);
        }

        public System.Type VisitAs(As @as)
        {
            return _proxy.Casting.GetSystemTypeFromTypeName(@as.TypeName);
        }
    }
}
