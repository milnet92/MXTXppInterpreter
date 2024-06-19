using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter
{
    /// <summary>
    /// Abstract syntax tree visitor interface
    /// </summary>
    public interface IAstVisitor
    {
        void VisitReturn(Return @return);
        void VisitFunctionDeclaration(FunctionDeclaration functionDeclaration);
        void VisitBreakpoint(Breakpoint breakpoint);
        void VisitInsertRecordset(InsertRecordset insertRecordset);
        void VisitDeleteFrom(DeleteFrom deleteFrom);
        void VisitUpdateRecordset(UpdateRecordset updateRecordset);
        void VisitThrow(Throw @throw);
        void VisitSelect(Select select);
        void VisitNext(Next next);
        void VisitWhileSelect(WhileSelect whileSelect);
        void VisitChangeCompany(ChangeCompany changeCompany);
        void VisitContainerInitialisation(ContainerInitialisation containerInitialisation);
        void VisitTtsCommit(TtsCommit ttsCommit);
        void VisitSwitch(Switch @switch);
        void VisitTtsBegin(TtsBegin ttsBegin);
        void VisitTtsAbort(TtsAbort ttsAbort);
        void VisitLoopControl(LoopControl loopControl);
        void VisitConstructor(Constructor constructor);
        void VisitAssignment(Assignment assignment);
        void VisitBinaryOperation(BinaryOperation binaryOperation);
        void VisitBlock(Block block);
        void VisitConstant(Constant constant);
        void VisitDo(Do @do);
        void VisitElse(Else @else);
        void VisitFor(For @for);
        void VisitNoReturnFunctionCall(NoReturnFunctionCall noReturnFunctionCall);
        void VisitProgram(Parser.Program program);
        void VisitIf(If @if);
        void VisitWhile(While @while);
        void VisitFunctionCall(FunctionCall functionCall);
        void VisitTernary(Ternary ternary);
        void VisitUnaryOperation(UnaryOperation unaryOperation);
        void VisitVariable(Variable variable);
        void VisitVariableDeclarations(VariableDeclarations variableDeclaration);
    }
}
