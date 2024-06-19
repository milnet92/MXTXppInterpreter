using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Lexer;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class ByteCodeGenerator : IAstVisitor
    {
        private readonly ByteCodeGenerationContext _generationContext = new ByteCodeGenerationContext();
        private readonly Stack<ByteCodeGenerationScope> _ss = new Stack<ByteCodeGenerationScope>();
        private readonly List<RefFunction> _declaredFunctions = new List<RefFunction>();
        private bool _generateDebugInfo;
        private bool _hasMaxIterations;

        public XppInterpreterOptions Options { get; }
        public ByteCodeGenerator(XppInterpreterOptions options = null)
        {
            _ss.Push(new ByteCodeGenerationScope());
            Options = options;

            _hasMaxIterations = Options != null && Options.MaxLoopIterations > 0;
        }

        public ByteCode Generate(Program program, bool generateDebugInfo)
        {
            if (_ss.Count == 0)
                _ss.Push(new ByteCodeGenerationScope());

            _generateDebugInfo = generateDebugInfo;

            VisitProgram(program);

            return new ByteCode(_ss.Pop().Instructions)
            {
                DeclaredFunctions = _declaredFunctions
            };

        }

        public ByteCode GetProgram()
        {
            return new ByteCode(_ss.Pop().Instructions)
            {
                DeclaredFunctions = _declaredFunctions
            };
        }

        public void Emit(IInstruction instruction, IDebuggeable debuggeable = null)
        {
            _ss.Peek().Instructions.Add(instruction);
        }

        public void EmitScope(ByteCodeGenerationScope scope, IDebuggeable debuggeable = null)
        {
            _ss.Peek().Instructions.AddRange(scope.Instructions);
        }

        public void EmitDebugSymbol(IDebuggeable debuggeable, bool always = false)
        {
            if (_generateDebugInfo && debuggeable.DebuggeableBinding != null)
            {
                Emit(new Debug(debuggeable, always));
            }
        }

        private void CreateScope()
        {
            var newScope = new ByteCodeGenerationScope();
            _ss.Push(newScope);
        }

        private ByteCodeGenerationScope ReleaseScope()
        {
            return _ss.Pop();
        }

        public void VisitInsertRecordset(InsertRecordset insertRecordset)
        {
            Emit(new InserQuery(insertRecordset));
        }

        public void VisitDeleteFrom(DeleteFrom deleteFrom)
        {
            Emit(new DeleteQuery(deleteFrom));
        }

        public void VisitUpdateRecordset(UpdateRecordset updateRecordset)
        {
            Emit(new UpdateQuery(updateRecordset));
        }

        public void VisitThrow(Throw @throw)
        {
            @throw.Exception.Accept(this);
            Emit(new ThrowException());
        }

        public void VisitSelect(Parser.Select select)
        {
            EmitDebugSymbol(select);
            Emit(new Select(select.Query, _generationContext.IsWhileSelectBeingGenerated(select)));
        }

        public void VisitNext(Parser.Next next)
        {
            EmitDebugSymbol(next);
            Emit(new VariableLoad(next.TableVariableName, false));
            Emit(new Next());
        }

        public void VisitWhileSelect(WhileSelect whileSelect)
        {
            _generationContext.AddWhileSelect(whileSelect.Select);

            EmitDebugSymbol(whileSelect);

            CreateScope();
            whileSelect.Select.Accept(this);
            var selectScope = ReleaseScope();

            CreateScope();
            whileSelect.Block.Accept(this);
            var blockScope = ReleaseScope();

            EmitScope(selectScope);
            Emit(new JumpIfFalse(blockScope.Count + 1 + 1 /*Jump instruction*/));
            EmitScope(blockScope);
            Emit(new Jump(-(blockScope.Count + selectScope.Count) - 1));

            _generationContext.RemoveWhileSelect(whileSelect.Select);

            this.RecalculateLoopControlOffsets(selectScope.Instructions.First());
        }

        public void VisitChangeCompany(Parser.ChangeCompany changeCompany)
        {
            changeCompany.Expression.Accept(this);
            Emit(new ChangeCompany());
            changeCompany.Block.Accept(this);
            Emit(new ChangeCompanyDispose());
        }

        public void VisitContainerInitialisation(ContainerInitialisation containerInitialisation)
        {
            for (int nElement = containerInitialisation.Elements.Count - 1; nElement >= 0; nElement --)
            {
                containerInitialisation.Elements[nElement].Accept(this);
            }

            Emit(new Container(containerInitialisation.Elements.Count));
        }

        public void VisitTtsCommit(Parser.TtsCommit ttsCommit)
        {
            EmitDebugSymbol(ttsCommit);
            Emit(new TtsCommit());
        }

        public void VisitTtsBegin(Parser.TtsBegin ttsBegin)
        {
            EmitDebugSymbol(ttsBegin);
            Emit(new TtsBegin());
        }

        public void VisitTtsAbort(Parser.TtsAbort ttsAbort)
        {
            EmitDebugSymbol(ttsAbort);
            Emit(new TtsAbort());
        }

        public void VisitLoopControl(Parser.LoopControl loopControl)
        {
            EmitDebugSymbol(loopControl);
            Emit(new LoopControl(0, loopControl.Token.TokenType, true));
        }

        public void VisitConstructor(Constructor constructor)
        {
            for (var narg = constructor.Parameters.Count - 1; narg >= 0; narg --)
            {
                constructor.Parameters[narg].Accept(this);
            }

            Emit(new NewObject(constructor.ClassName, constructor.Parameters.Count, true));
        }

        public void VisitAssignment(Assignment assignment)
        {
            EmitDebugSymbol(assignment);

            assignment.Expression.Accept(this);

            bool fromCaller = false;

            if (assignment.Assignee.Caller != null)
            {
                fromCaller = true;
                assignment.Assignee.Caller.Accept(this);
            }

            bool isArray = false;

            if (assignment.Assignee is ArrayAccess access)
            {
                isArray = true;
                access.Index.Accept(this);
            }

            Emit(new Store(assignment.Assignee.Name, fromCaller, false, isArray, null));
        }

        public void VisitBinaryOperation(BinaryOperation binaryOperation)
        {
            binaryOperation.LeftOperand.Accept(this);
            var tokenType = binaryOperation.Token.TokenType;

            if (Arithmetic.IsArithmetic(tokenType))
            {
                binaryOperation.RightOperand.Accept(this);

                Emit(new Arithmetic(binaryOperation.Token.TokenType));
            }
            else
            {
                // Create scope for left operand
                CreateScope();
                binaryOperation.RightOperand.Accept(this);
                var scope = ReleaseScope();

                if (tokenType == TType.And)
                    Emit(new JumpIfFalse(scope.Count + 1));
                else
                    Emit(new JumpIfTrue(scope.Count + 1));

                EmitScope(scope);
            }
        }

        public void VisitBlock(Block block)
        {
            EmitDebugSymbol(block);
            Emit(new BeginScope());

            foreach (var statement in block.Statements)
            {
                statement.Accept(this);
            }

            Emit(new EndScope());
        }

        public void VisitConstant(Constant constant)
        {
            object value = null;

            if (constant.Token is BaseType baseType)
                value = baseType.Value;
            else if (constant.Token.TokenType == TType.True)
                value = true;
            else if (constant.Token.TokenType == TType.False)
                value = false;

            Emit(new Push(value));
        }

        public void VisitDo(Do @do)
        {
            EmitDebugSymbol(@do);

            CreateScope();
            @do.Block.Accept(this);
            @do.Expression.Accept(this);
            var scope = ReleaseScope();
            
            EmitScope(scope);
            Emit(new JumpIfTrue(-(scope.Count)));
        }

        public void VisitElse(Else @else)
        {
            EmitDebugSymbol(@else);

            VisitIf(@else);
        }

        public void VisitFor(For @for)
        {
            EmitDebugSymbol(@for);

            Emit(new BeginScope());

            @for.Initialisation?.Accept(this);

            CreateScope();
            @for.Condition?.Accept(this);
            var forScope = ReleaseScope();

            CreateScope();
            @for.Block.Accept(this);
            var blockScope = ReleaseScope();

            CreateScope();
            @for.LoopStatement.Accept(this);
            var loopScope = ReleaseScope();

            CreateScope();
            if (_hasMaxIterations)
                Emit(new MaxLoopThrow());
            var maxLoopScope = ReleaseScope();

            if (_hasMaxIterations)
                Emit(new Push(Options.MaxLoopIterations));

            EmitScope(forScope);
            Emit(new JumpIfFalse(maxLoopScope.Count + blockScope.Count + loopScope.Count + 1 + 1 /* Jump instruction */));
            EmitScope(maxLoopScope);
            EmitScope(blockScope);
            EmitScope(loopScope);
            Emit(new Jump(- (maxLoopScope.Count + blockScope.Count + loopScope.Count + forScope.Count + 1)));

            RecalculateLoopControlOffsets(loopScope.Instructions.First());

            Emit(new EndScope());

        }

        public void VisitNoReturnFunctionCall(NoReturnFunctionCall noReturnFunctionCall)
        {
            EmitDebugSymbol(noReturnFunctionCall);
            VisitFunctionCall(noReturnFunctionCall.FunctionCall, false);
        }

        public void VisitProgram(Program program)
        {
            foreach (var statement in program.Statements)
            {
                statement.Accept(this);
            }
        }

        public void VisitIf(If @if)
        {
            EmitDebugSymbol(@if);

            CreateScope();
            @if.Expression.Accept(this);
            var expressionScope = ReleaseScope();

            CreateScope();
            @if.Statement.Accept(this);
            var statementScope = ReleaseScope();

            ByteCodeGenerationScope elseScope = null;
            if (@if.Else != null)
            {
                CreateScope();
                @if.Else.Accept(this);
                elseScope = ReleaseScope();
            }

            EmitScope(expressionScope);

            if (elseScope != null)
            {
                Emit(new JumpIfFalse(statementScope.Count + 2));
            }
            else
            {
                Emit(new JumpIfFalse(statementScope.Count + 1));
            }

            EmitScope(statementScope);

            if (elseScope != null)
            {
                Emit(new Jump(elseScope.Count + 1));
                EmitScope(elseScope);
            }
        }

        public void VisitWhile(While @while)
        {
            EmitDebugSymbol(@while);

            CreateScope();
            @while.Expression.Accept(this);
            var expressionScope = ReleaseScope();

            CreateScope();
            @while.Block.Accept(this);
            var blockScope = ReleaseScope();

            CreateScope();
            if (_hasMaxIterations)
                Emit(new MaxLoopThrow());
            var loopCheckScope = ReleaseScope();

            if (_hasMaxIterations)
                Emit(new Push(Options.MaxLoopIterations));

            EmitScope(expressionScope);
            Emit(new JumpIfFalse(loopCheckScope.Count + blockScope.Count + 1 + 1 /*Jump instruction*/));
            EmitScope(loopCheckScope);
            EmitScope(blockScope);
            Emit(new Jump(-(loopCheckScope.Count + blockScope.Count + expressionScope.Count) - 1));

            RecalculateLoopControlOffsets(expressionScope.Instructions.First());
        }

        private void RecalculateLoopControlOffsets(IInstruction jumpInstructionForContinue)
        {
            // Move the pending loop control jumps
            foreach (LoopControl loopControl in _ss.Peek().Instructions.Where(i => i is LoopControl lc && lc.IsDirty))
            {
                if (loopControl.TokenType == TType.Break)
                {
                    int offset = _ss.Peek().Instructions.Count - _ss.Peek().Instructions.IndexOf(loopControl);
                    loopControl.SetFinalOffset(offset);
                }
                else if (loopControl.TokenType == TType.Continue && jumpInstructionForContinue != null)
                {
                    int offset = -(_ss.Peek().Instructions.IndexOf(loopControl) - _ss.Peek().Instructions.IndexOf(jumpInstructionForContinue));
                    loopControl.SetFinalOffset(offset);
                }
            }
        }

        public void VisitFunctionCall(FunctionCall functionCall)
        {
            EmitDebugSymbol(functionCall);
            VisitFunctionCall(functionCall, true);
        }

        private void VisitFunctionCall(FunctionCall functionCall, bool allocate)
        {
            for (var narg = functionCall.Parameters.Count - 1; narg >= 0; narg--)
            {
                functionCall.Parameters[narg].Accept(this);
            }

            int nArgs = functionCall.Parameters.Count;

            if (_declaredFunctions.Exists(f => f.Declaration.Name.ToLowerInvariant() == functionCall.Name.ToLowerInvariant()) && functionCall.Caller is null)
            {
                var declaredFunctionRef = _declaredFunctions.First(f => f.Declaration.Name.ToLowerInvariant() == functionCall.Name.ToLowerInvariant());
                Emit(new DeclaredFunctionCall(declaredFunctionRef));
            }
            else if (functionCall.Intrinsical)
            {
                Emit(new IntrinsicCall(functionCall.Name, nArgs, allocate));
            }
            else if (functionCall.Caller is null)
            {
                Emit(new StaticFunctionCall(functionCall.Name, nArgs, allocate));
            }
            else if (functionCall.StaticCall)
            {
                string callerName = ((Word)(functionCall.Caller as Variable).Token).Lexeme;
                Emit(new StaticFunctionCall(functionCall.Name, nArgs, allocate, callerName));
            }
            else
            {
                functionCall.Caller.Accept(this);
                Emit(new InstanceFunctionCall(functionCall.Name, nArgs, allocate));
            }
        }

        public void VisitTernary(Ternary ternary)
        {
            EmitDebugSymbol(ternary);
            ternary.Condition.Accept(this);
            
            CreateScope();
            ternary.Left.Accept(this);
            var leftScope = ReleaseScope();

            CreateScope();
            ternary.Right.Accept(this);
            var rightScope = ReleaseScope();

            Emit(new JumpIfFalse(leftScope.Count + 1 + 1 /*Jump instruction */));
            EmitScope(leftScope);
            Emit(new Jump(rightScope.Count + 1));
            EmitScope(rightScope);
        }

        public void VisitUnaryOperation(UnaryOperation unaryOperation)
        {
            unaryOperation.Expression.Accept(this);

            switch (unaryOperation.Token.TokenType)
            {
                case TType.Negation:
                    Emit(new Negation());
                    break;

                case TType.Minus:
                    Emit(new Push(-1));
                    Emit(new Arithmetic(TType.Star));
                    break;
            }
        }

        public void VisitVariable(Variable variable)
        {
            bool isArray = false;

            if (variable is ArrayAccess arrayAccess)
            {
                arrayAccess.Index.Accept(this);
                isArray = true;
            }

            if (variable.Caller is null)
            {
                Emit(new VariableLoad(variable.Name, isArray));
            }
            else if (!variable.StaticCall)
            {
                variable.Caller.Accept(this);
                Emit(new InstanceLoad(variable.Name, isArray));
            }
            else
            {
                string callerName = (variable.Caller.Token as Word).Lexeme;
                Emit(new StaticLoad(variable.Name, callerName, isArray));
            }
        }

        public void VisitVariableDeclarations(VariableDeclarations variableDeclarations)
        {
            EmitDebugSymbol(variableDeclarations);
            foreach (var declaration in variableDeclarations.Identifiers)
            {
                if (declaration.Value != null)
                {
                    declaration.Value.Accept(this);
                }
                else if (variableDeclarations is VariableArrayDeclaration arrayDeclaration)
                {
                    arrayDeclaration.Size?.Accept(this);
                    Emit(new DefaultValueArray(variableDeclarations.VariableType.Lexeme, arrayDeclaration.Size != null));
                }
                else
                {
                    Emit(new DefaultValue(variableDeclarations.VariableType.Lexeme));
                }

                Emit(new Store(declaration.Key.Lexeme, false, true, false, variableDeclarations.VariableType.Lexeme));
            }
        }

        public void VisitBreakpoint(Parser.Breakpoint breakpoint)
        {
            EmitDebugSymbol(breakpoint, true);
        }

        public void VisitSwitch(Switch @switch)
        {
            EmitDebugSymbol(@switch);

            CreateScope();
            @switch.Expression.Accept(this);
            var expressionScope = ReleaseScope();

            var caseScopes = new Dictionary<ByteCodeGenerationScope, ByteCodeGenerationScope>();

            // Emit cases
            foreach (var @case in @switch.Cases)
            {
                CreateScope();
                EmitDebugSymbol(@case.Key);
                @case.Key.Accept(this);
                // delete all debug symbols that are created by the expression itself
                DeleteAllDebugSymbolsInScope(true); 
                var caseExpressionScope = ReleaseScope();

                CreateScope();
                foreach (var caseStatement in @case.Value)
                {
                    caseStatement.Accept(this);
                }
                var caseStatementsScope = ReleaseScope();

                caseScopes.Add(caseExpressionScope, caseStatementsScope);
            }

            // Emit default
            CreateScope();
            foreach (var defaultStatement in @switch.Default)
            {
                defaultStatement.Accept(this);
            }
            var defaultScope = ReleaseScope();

            Emit(new BeginScope());

            EmitScope(expressionScope);

            foreach (var caseScope in caseScopes)
            {
                EmitScope(caseScope.Key);
                Emit(new Case(caseScope.Value.Count + 1));
                EmitScope(caseScope.Value);
            }

            EmitScope(defaultScope);

            RecalculateLoopControlOffsets(null);

            Emit(new EndScope());
        }

        private void DeleteAllDebugSymbolsInScope(bool skipFirst)
        {
            var instructions = _ss.Peek().Instructions.Where(ins => ins is Debug).Reverse();
            int lastIndex = skipFirst ? 1 : 0;

            int total = instructions.Count();
            foreach (var instruction in instructions)
            {
                total--;

                if (skipFirst && total == 0) break;
                
                _ss.Peek().Instructions.Remove(instruction);
            }
        }

        public void VisitFunctionDeclaration(FunctionDeclaration functionDeclaration)
        {
            // Create dummy function
            RefFunction refFunction = new RefFunction(functionDeclaration);
            _declaredFunctions.Add(refFunction);

            CreateScope();
            functionDeclaration.Block.Accept(this);
            var blockScope = ReleaseScope();

            refFunction.Instructions = blockScope.Instructions;
        }

        public void VisitReturn(Parser.Return @return)
        {
            EmitDebugSymbol(@return);
            @return.Expression.Accept(this);
            Emit(new Return());
        }

        public void VisitPrint(Print print)
        {
            EmitDebugSymbol(print);

            // Print call is translated into strFmt and info calls
            StringBuilder sb = new StringBuilder();
            for (var narg = print.Parameters.Count - 1; narg >= 0; narg--)
            {
                if (narg != print.Parameters.Count - 1)
                    sb.Append(' ');
                
                sb.Append($"%{print.Parameters.Count - narg}");
                print.Parameters[narg].Accept(this);
            }

            Emit(new Push(sb.ToString()));
            Emit(new StaticFunctionCall("strFmt", print.Parameters.Count + 1, true));
            Emit(new StaticFunctionCall("info", 1, false));
        }
    }
}
