using System;

namespace XppInterpreter.Parser.Completer
{
    internal interface ITypeInferExpressionVisitor
    {
        Type VisitContainerInitialisation(ContainerInitialisation containerInitialisation);
        Type VisitConstructor(Constructor constructor);
        Type VisitBinaryOperation(BinaryOperation binaryOperation);
        Type VisitConstant(Constant constant);
        Type VisitTernary(Ternary ternary);
        Type VisitUnaryOperation(UnaryOperation unaryOperation);
        Type VisitVariable(Variable variable);
        Type VisitFunctionCall(FunctionCall functionCall);
    }
}
