using System;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter
{
    public interface IExpressionVisitor
    {
        void VisitContainerInitialisation(ContainerInitialisation containerInitialisation);
        void VisitConstructor(Constructor constructor);
        void VisitBinaryOperation(BinaryOperation binaryOperation);
        void VisitConstant(Constant constant);
        void VisitTernary(Ternary ternary);
        void VisitUnaryOperation(UnaryOperation unaryOperation);
        void VisitVariable(Variable variable);
        void VisitIs(Is @is);
        void VisitAs(As @as);
        void VisitSelectExpression(SelectExpression selectExpression);
        void VisitTableField(TableField tableField);
    }
}
