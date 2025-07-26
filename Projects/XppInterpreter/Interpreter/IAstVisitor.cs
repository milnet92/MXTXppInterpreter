using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter
{
    /// <summary>
    /// Abstract syntax tree visitor interface
    /// </summary>
    public interface IAstVisitor : IExpressionVisitor
    {
        void VisitPrint(Print print);
        void VisitReturn(Return @return);
        void VisitFunctionDeclaration(FunctionDeclaration functionDeclaration);
        void VisitBreakpoint(Breakpoint breakpoint);
        void VisitInsertRecordset(InsertRecordset insertRecordset);
        void VisitDeleteFrom(DeleteFrom deleteFrom);
        void VisitUpdateRecordset(UpdateRecordset updateRecordset);
        void VisitThrow(Throw @throw);
        void VisitSelect(Select select);
        void VisitNext(Next next);
        void VisitFlush(Flush flush);
        void VisitWhileSelect(WhileSelect whileSelect);
        void VisitUnchecked(Unchecked @unchecked);
        void VisitChangeCompany(ChangeCompany changeCompany);
        void VisitTtsCommit(TtsCommit ttsCommit);
        void VisitContainerAssignment(ContainerAssignment containerAssignment);
        void VisitSwitch(Switch @switch);
        void VisitTtsBegin(TtsBegin ttsBegin);
        void VisitTtsAbort(TtsAbort ttsAbort);
        void VisitLoopControl(LoopControl loopControl);
        void VisitAssignment(Assignment assignment);
        void VisitBlock(Block block);
        void VisitDo(Do @do);
        void VisitElse(Else @else);
        void VisitFor(For @for);
        void VisitNoReturnFunctionCall(NoReturnFunctionCall noReturnFunctionCall);
        void VisitProgram(Parser.Program program);
        void VisitIf(If @if);
        void VisitWhile(While @while);
        void VisitFunctionCall(FunctionCall functionCall);
        void VisitEventHandlerSubscription(EventHandlerSubscription subscription);
        void VisitVariableDeclarations(VariableDeclarations variableDeclaration);
        void VisitTry(Try @try);
        void VisitRetry(Retry retry);
        void VisitUsing(Using @using);
    }
}
