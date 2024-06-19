using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter
{
    /// <summary>
    /// Base class for AST visitors that collect inforamtion
    /// </summary>
    public class AstSimpleVisitor : IAstVisitor
    {
        public virtual void VisitPrint(Print print)
        {
            foreach (var parameter in print.Parameters)
            {
                parameter.Accept(this);
            }
        }

        public virtual void VisitReturn(Return @return)
        {
            @return.Expression?.Accept(this);
        }

        public virtual void VisitAssignment(Assignment assignment)
        {
            assignment.Assignee?.Accept(this);
            assignment.Expression?.Accept(this);
        }

        public virtual void VisitBinaryOperation(BinaryOperation binaryOperation)
        {
            binaryOperation.LeftOperand?.Accept(this);
            binaryOperation.RightOperand?.Accept(this);
        }

        public virtual void VisitBlock(Block block)
        {
            foreach (var stmt in block.Statements)
            {
                stmt.Accept(this);
            }
        }

        public virtual void VisitBreakpoint(Breakpoint breakpoint)
        {

        }

        public virtual void VisitChangeCompany(ChangeCompany changeCompany)
        {
            changeCompany.Expression.Accept(this);
            changeCompany.Block.Accept(this);
        }

        public virtual void VisitConstant(Constant constant)
        {
        }

        public virtual void VisitConstructor(Constructor constructor)
        {
            constructor.Caller?.Accept(this);
            foreach (var expr in constructor.Parameters)
            {
                expr.Accept(this);
            }
        }

        public virtual void VisitContainerInitialisation(ContainerInitialisation containerInitialisation)
        {
            foreach (var element in containerInitialisation.Elements)
            {
                element.Accept(this);
            }
        }

        public virtual void VisitDeleteFrom(DeleteFrom deleteFrom)
        {
        }

        public virtual void VisitDo(Do @do)
        {
            @do.Block?.Accept(this);
            @do.Expression?.Accept(this);
        }

        public virtual void VisitElse(Else @else)
        {
            @else.Else?.Accept(this);
            @else.Expression?.Accept(this);
            @else.Statement?.Accept(this);
        }

        public virtual void VisitFor(For @for)
        {
            @for.Initialisation?.Accept(this);
            @for.Condition?.Accept(this);
            @for.LoopStatement?.Accept(this);
            @for.Block?.Accept(this);
        }

        public virtual void VisitFunctionCall(FunctionCall functionCall)
        {
            functionCall.Caller?.Accept(this);
            foreach (var parameter in functionCall.Parameters)
            {
                parameter.Accept(this);
            }
        }

        public void VisitFunctionDeclaration(FunctionDeclaration functionDeclaration)
        {
            functionDeclaration.Block.Accept(this);
        }

        public virtual void VisitIf(If @if)
        {
            @if.Expression?.Accept(this);
            @if.Statement?.Accept(this);
            @if.Else?.Accept(this);
        }

        public virtual void VisitInsertRecordset(InsertRecordset insertRecordset)
        {
        }

        public virtual void VisitLoopControl(LoopControl loopControl)
        {
        }

        public virtual void VisitNext(Next next)
        {
        }

        public virtual void VisitNoReturnFunctionCall(NoReturnFunctionCall noReturnFunctionCall)
        {
            noReturnFunctionCall.FunctionCall?.Accept(this);
        }

        public virtual void VisitProgram(Program program)
        {
            foreach (var stmt in program.Statements)
            {
                stmt.Accept(this);
            }
        }

        public virtual void VisitSelect(Select select)
        {
        }

        public virtual void VisitSwitch(Switch @switch)
        {
            @switch.Expression.Accept(this);

            if (@switch.Cases != null)
            {
                foreach (var cse in @switch.Cases)
                {
                    cse.Key.Accept(this);

                    foreach (var stmt in cse.Value)
                    {
                        stmt.Accept(this);
                    }
                }
            }

            if (@switch.Default != null)
            {
                foreach (var stmt in @switch.Default)
                {
                    stmt.Accept(this);
                }
            }
        }

        public virtual void VisitTernary(Ternary ternary)
        {
            ternary.Condition?.Accept(this);
            ternary.Left?.Accept(this);
            ternary.Right?.Accept(this);
            
        }

        public virtual void VisitThrow(Throw @throw)
        {
            @throw.Exception?.Accept(this);
        }

        public virtual void VisitTtsAbort(TtsAbort ttsAbort)
        {
        }

        public virtual void VisitTtsBegin(TtsBegin ttsBegin)
        {
        }

        public virtual void VisitTtsCommit(TtsCommit ttsCommit)
        {
        }

        public virtual void VisitUnaryOperation(UnaryOperation unaryOperation)
        {
            unaryOperation.Expression?.Accept(this);
        }

        public virtual void VisitUpdateRecordset(UpdateRecordset updateRecordset)
        {
        }

        public virtual void VisitVariable(Variable variable)
        {
            variable.Caller?.Accept(this);
        }

        public virtual void VisitVariableDeclarations(VariableDeclarations variableDeclaration)
        {
            foreach (var expr in variableDeclaration.Identifiers)
            {
                expr.Value?.Accept(this);
            }
        }

        public virtual void VisitWhile(While @while)
        {
            @while.Expression?.Accept(this);
            @while.Block?.Accept(this);
        }

        public virtual void VisitWhileSelect(WhileSelect whileSelect)
        {
            whileSelect.Select?.Accept(this);
            whileSelect.Block.Accept(this);
        }
    }
}
