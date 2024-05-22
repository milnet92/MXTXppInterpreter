using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter.Query
{
    public interface IQueryExpressionVisitor
    {
        Dynamics.AX.Application.SysDaQueryExpression Visit(BinaryOperation binaryOperation);
        Dynamics.AX.Application.SysDaQueryExpression Visit(UnaryOperation unaryOperation);
        Dynamics.AX.Application.SysDaQueryExpression Visit(Constant constant);
        Dynamics.AX.Application.SysDaQueryExpression Visit(Variable variable);
    }
}
