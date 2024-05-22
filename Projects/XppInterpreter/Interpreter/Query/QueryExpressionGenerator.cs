using Dynamics.AX.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter.Query
{
    public class QueryExpressionGenerator : IQueryExpressionVisitor
    {
        private readonly List<string> _tablesInQuery;
        private readonly RuntimeContext _context;
        private readonly QueryGenerationHelper _helper;
        public QueryExpressionGenerator(RuntimeContext context, List<string> tablesInQuery)
        {
            _context = context;
            _tablesInQuery = tablesInQuery;
            _helper = new QueryGenerationHelper(context);
        }

        public SysDaQueryExpression Visit(Expression expression)
        {
            if (expression is BinaryOperation binaryOperation)
            {
                return Visit(binaryOperation);
            }
            else if (expression is UnaryOperation unaryOperation)
            {
                return Visit(unaryOperation);
            }
            else if (expression is Constant constant)
            {
                return Visit(constant);
            }
            else if (expression is Variable variable)
            {
                return Visit(variable);
            }

            throw new NotImplementedException();
        }

        public SysDaQueryExpression Visit(BinaryOperation binaryOperation)
        {
            var left = Visit(binaryOperation.LeftOperand);
            var right = Visit(binaryOperation.RightOperand);

            switch (binaryOperation.Token.TokenType)
            {
                case TType.And:
                    return new SysDaAndExpression(left, right);

                case TType.Or:
                    return new SysDaOrExpression(left, right);

                case TType.Equal:
                    return new SysDaEqualsExpression(left, right);

                case TType.NotEqual:
                    return new SysDaNotEqualsExpression(left, right);

                case TType.Greater:
                    return new SysDaGreaterThanExpression(left, right);

                case TType.GreaterOrEqual:
                    return new SysDaGreaterThanOrEqualsExpression(left, right);

                case TType.Smaller:
                    return new SysDaLessThanExpression(left, right);

                case TType.SmallerOrEqual:
                    return new SysDaLessThanOrEqualsExpression(left, right);

                case TType.Plus:
                    return new SysDaPlusExpression(left, right);

                case TType.Minus:
                    return new SysDaMinusExpression(left, right);

                case TType.Division:
                    return new SysDaDivideExpression(left, right);

                case TType.Star:
                    return new SysDaMultiplyExpression(left, right);

                case TType.Mod:
                    return new SysDaModExpression(left, right);

                case TType.IntegerDivision:
                    return new SysDaIntDivExpression(left, right);

                case TType.In:
                    return new SysDaInExpression(left, right);

                case TType.Like:
                    return new SysDaLikeExpression(left, right);
            }

            throw new System.Exception("Invalid binary expression.");
        }

        public SysDaQueryExpression Visit(UnaryOperation unaryOperation)
        {
            throw new NotImplementedException("Unary expressions for query statements are not implemented.");
        }

        public SysDaQueryExpression Visit(Constant constant)
        {
            return new SysDaValueExpression(_helper.ComputeConstant(constant));
        }

        public SysDaQueryExpression Visit(Variable variable)
        {
            if (variable.Caller != null && variable.Caller.Token is Word w && _tablesInQuery.Any(t => t.ToLowerInvariant() == w.Lexeme.ToLowerInvariant()))
            {
                var common = (Microsoft.Dynamics.Ax.Xpp.Common)_helper.ComputeVariable((Variable)variable.Caller);

                return new SysDaFieldExpression(common, (variable.Token as Word).Lexeme);
            }
            if (variable is FunctionCall funcCall)
            {
                return new SysDaValueExpression(_helper.ComputeFunctionCall(funcCall));
            }
            else
            {
                return new SysDaValueExpression(_helper.ComputeVariable(variable));
            }
        }
    }
}
