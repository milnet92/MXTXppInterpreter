using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter.Debug
{
    /// <summary>
    /// This visitor is used to traverse through the AST and collect
    /// information on what elements a breakpoint can be set
    /// </summary>
    public class AST2SourceCodeBindableCollection : AstSimpleVisitor
    {
        private List<IDebuggeable> _bindings = new List<IDebuggeable>();
        private bool _hasBeenGeneratd = false;

        private Stack<bool> _functionStackScope = new Stack<bool>();

        public Program Program { get; }

        public AST2SourceCodeBindableCollection(Program program)
        {
            Program = program;
        }

        /// <summary>
        /// Generates the list of <see cref="IDebuggeable"/> elements
        /// that a breakpoint can be set to
        /// </summary>
        /// <returns>A List of <see cref="IDebuggeable"/></returns>
        public List<IDebuggeable> Generate()
        {
            if (!_hasBeenGeneratd)
            {
                VisitProgram(Program);
                _hasBeenGeneratd = true;
            }

            return _bindings;
        }
        
        /// <summary>
        /// Tries to add an element to the generation list based on the type and scope
        /// </summary>
        /// <param name="obj">Object to add</param>
        private void AddToCollection(object obj)
        {
            if (_hasBeenGeneratd) return;

            if (obj != null && obj is IDebuggeable debuggeable && debuggeable.DebuggeableBinding != null && _functionStackScope.Count == 0)
                _bindings.Add((IDebuggeable)obj);
        }
        public override void VisitReturn(Return @return)
        {
            AddToCollection(@return);
            base.VisitReturn(@return);
        }

        public override void VisitAssignment(Assignment assignment)
        {
            AddToCollection(assignment);
            base.VisitAssignment(assignment);
        }

        public override void VisitBinaryOperation(BinaryOperation binaryOperation)
        {
            AddToCollection(binaryOperation);
            base.VisitBinaryOperation(binaryOperation);
        }

        public override void VisitBlock(Block block)
        {
            AddToCollection(block);
            base.VisitBlock(block);
        }

        public override void VisitChangeCompany(ChangeCompany changeCompany)
        {
            AddToCollection(changeCompany);
            base.VisitChangeCompany(changeCompany);
        }

        public override void VisitConstant(Constant constant)
        {
            AddToCollection(constant);
            base.VisitConstant(constant);
        }

        public override void VisitConstructor(Constructor constructor)
        {
            AddToCollection(constructor);
            base.VisitConstructor(constructor);
        }

        public override void VisitContainerInitialisation(ContainerInitialisation containerInitialisation)
        {  
            AddToCollection(containerInitialisation);
            base.VisitContainerInitialisation(containerInitialisation);
        }

        public override void VisitDeleteFrom(DeleteFrom deleteFrom)
        {
            AddToCollection(deleteFrom);
            base.VisitDeleteFrom(deleteFrom);
        }

        public override void VisitDo(Do @do)
        {
            AddToCollection(@do);
            base.VisitDo(@do);
        }

        public override void VisitElse(Else @else)
        {
            AddToCollection(@else);
            base.VisitElse(@else);
        }

        public override void VisitFor(For @for)
        {
            AddToCollection(@for);
            base.VisitFor(@for);
        }

        public override void VisitFunctionCall(FunctionCall functionCall)
        {
            AddToCollection(functionCall);

            _functionStackScope.Push(true);
            base.VisitFunctionCall(functionCall);
            _functionStackScope.Pop();
        }

        public override void VisitIf(If @if)
        {
            AddToCollection(@if);
            base.VisitIf(@if);
        }

        public override void VisitInsertRecordset(InsertRecordset insertRecordset)
        {
            AddToCollection(insertRecordset);
            base.VisitInsertRecordset(insertRecordset);
        }

        public override void VisitLoopControl(LoopControl loopControl)
        {
            AddToCollection(loopControl);
            base.VisitLoopControl(loopControl);
        }

        public override void VisitNext(Next next)
        {
            AddToCollection(next);
            base.VisitNext(next);
        }

        public override void VisitBreakpoint(Parser.Breakpoint breakpoint)
        {
            AddToCollection(breakpoint);
            base.VisitBreakpoint(breakpoint);
        }

        public override void VisitNoReturnFunctionCall(NoReturnFunctionCall noReturnFunctionCall)
        {
            AddToCollection(noReturnFunctionCall);
            _functionStackScope.Push(true);
            base.VisitNoReturnFunctionCall(noReturnFunctionCall);
            _functionStackScope.Pop();
        }

        public override void VisitProgram(Program program)
        {
            AddToCollection(program);
            base.VisitProgram(program);
        }

        public override void VisitSelect(Select select)
        {
            AddToCollection(select);
            base.VisitSelect(select);
        }

        public override void VisitTernary(Ternary ternary)
        {
            AddToCollection(ternary);
            base.VisitTernary(ternary);
        }

        public override void VisitThrow(Throw @throw)
        {
            AddToCollection(@throw);
            base.VisitThrow(@throw);
        }

        public override void VisitTtsAbort(TtsAbort ttsAbort)
        {
            AddToCollection(ttsAbort);
            base.VisitTtsAbort(ttsAbort);
        }

        public override void VisitTtsBegin(TtsBegin ttsBegin)
        {
            AddToCollection(ttsBegin);
            base.VisitTtsBegin(ttsBegin);
        }

        public override void VisitTtsCommit(TtsCommit ttsCommit)
        {
            AddToCollection(ttsCommit);
            base.VisitTtsCommit(ttsCommit);
        }

        public override void VisitUnaryOperation(UnaryOperation unaryOperation)
        {
            AddToCollection(unaryOperation);
            base.VisitUnaryOperation(unaryOperation);
        }

        public override void VisitUpdateRecordset(UpdateRecordset updateRecordset)
        {
            AddToCollection(updateRecordset);
            base.VisitUpdateRecordset(updateRecordset);
        }

        public override void VisitVariable(Variable variable)
        {
            AddToCollection(variable);
            base.VisitVariable(variable);
        }

        public override void VisitVariableDeclarations(VariableDeclarations variableDeclaration)
        {
            AddToCollection(variableDeclaration);
            base.VisitVariableDeclarations(variableDeclaration);
        }

        public override void VisitWhile(While @while)
        {
            AddToCollection(@while);
            base.VisitWhile(@while);
        }

        public override void VisitWhileSelect(WhileSelect whileSelect)
        {
            AddToCollection(whileSelect);
            base.VisitWhileSelect(whileSelect);
        }

        public override void VisitSwitch(Switch @switch)
        {
            AddToCollection(@switch);
            base.VisitSwitch(@switch);
        }

        public override void VisitPrint(Print print)
        {
            AddToCollection(print);
            base.VisitPrint(print);
        }
    }
}
