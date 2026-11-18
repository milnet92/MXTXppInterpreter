using System.Collections.Generic;
using System.Linq;
using System.Text;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Lexer;
using XppInterpreter.Parser;
using XppInterpreter.Parser.Data;

namespace XppInterpreter.Interpreter.Bytecode
{
    public class ByteCodeGenerator : IAstVisitor
    {
        private readonly ByteCodeGenerationContext _generationContext = new ByteCodeGenerationContext();
        private readonly Stack<ByteCodeGenerationScope> _ss = new Stack<ByteCodeGenerationScope>();
        private readonly List<RefFunction> _declaredFunctions = new List<RefFunction>();
        private readonly bool _hasMaxIterations;
        private bool _generateDebugInfo;

        // How many try statements enclose the code being generated, and how many enclosed the
        // statements a break or a continue goes to: the difference is the handlers they leave.
        private int _tryDepth;
        private readonly Stack<int> _breakTargets = new Stack<int>();
        private readonly Stack<int> _continueTargets = new Stack<int>();

        public XppInterpreterOptions Options { get; }

        public ByteCodeGenerator(XppInterpreterOptions options = null, List<RefFunction> declaredFunctions = null)
        {
            _ss.Push(new ByteCodeGenerationScope());

            Options = options;
            _hasMaxIterations = Options != null && Options.MaxLoopIterations > 0;

            if (declaredFunctions != null)
            {
                _declaredFunctions.AddRange(declaredFunctions);
            }
        }

        public ByteCode GeneratedByteCode => new ByteCode(_ss.Peek().Instructions)
        {
            DeclaredFunctions = _declaredFunctions
        };

        public ByteCode Generate(Program program, bool generateDebugInfo, List<RefFunction> dependencyFunctions = null)
        {
            if (dependencyFunctions != null)
            {
                _declaredFunctions.AddRange(dependencyFunctions);
            }

            if (_ss.Count == 0)
            {
                _ss.Push(new ByteCodeGenerationScope());
            }

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

        public void Emit(IInstruction instruction)
        {
            _ss.Peek().Instructions.Add(instruction);
        }

        public void EmitScope(ByteCodeGenerationScope scope)
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
            EmitDebugSymbol(insertRecordset);
            Emit(new InsertQuery(insertRecordset));
        }

        public void VisitDeleteFrom(DeleteFrom deleteFrom)
        {
            EmitDebugSymbol(deleteFrom);
            Emit(new DeleteQuery(deleteFrom));
        }

        public void VisitUpdateRecordset(UpdateRecordset updateRecordset)
        {
            EmitDebugSymbol(updateRecordset);
            Emit(new UpdateQuery(updateRecordset));
        }

        public void VisitThrow(Throw @throw)
        {
            EmitDebugSymbol(@throw);
            @throw.Exception.Accept(this);
            Emit(new ThrowException());
        }

        public void VisitSelect(Parser.Select select)
        {
            EmitDebugSymbol(select);
            Emit(new Select(select.Query, _generationContext.IsWhileSelectBeingGenerated(select)));
        }

        public void VisitSelectExpression(Parser.SelectExpression selectExpression)
        {
            EmitDebugSymbol(selectExpression);
            Emit(new Select(selectExpression.Query, returnField: selectExpression.ReturnField));
        }

        public void VisitNext(Parser.Next next)
        {
            EmitDebugSymbol(next);
            Emit(new VariableLoad(next.TableVariableName, false));
            Emit(new Next());
        }

        public void VisitFlush(Parser.Flush flush)
        {
            EmitDebugSymbol(flush);
            Emit(new Flush(flush.TableName));
        }

        public void VisitWhileSelect(WhileSelect whileSelect)
        {
            _generationContext.AddWhileSelect(whileSelect.Select);

            EmitDebugSymbol(whileSelect);

            CreateScope();
            whileSelect.Select.Accept(this);
            var selectScope = ReleaseScope();

            BeginLoop();
            CreateScope();
            whileSelect.Block.Accept(this);
            var blockScope = ReleaseScope();
            EndLoop();

            Emit(new ResetQuery(whileSelect.Select.Query));
            EmitScope(selectScope);
            Emit(new JumpIfFalse(blockScope.Count + 1 + 1 /*Jump instruction*/, false));
            EmitScope(blockScope);
            Emit(new Jump(-(blockScope.Count + selectScope.Count) - 1));

            _generationContext.RemoveWhileSelect(whileSelect.Select);

            this.RecalculateLoopControlOffsets(selectScope.Instructions.First());
        }

        public void VisitUnchecked(Parser.Unchecked @unchecked)
        {
            @unchecked.Expression.Accept(this);
            Emit(new Unchecked());
            EmitTry(new Try(@unchecked.Block, @unchecked.Block.SourceCodeBinding), new DisposeHandle());
        }

        public void VisitChangeCompany(Parser.ChangeCompany changeCompany)
        {
            changeCompany.Expression.Accept(this);
            Emit(new ChangeCompany());

            // The company comes back however the block ends: a return, a break or an exception
            EmitTry(new Try(changeCompany.Block, changeCompany.Block.SourceCodeBinding), new DisposeHandle());
        }

        public void VisitContainerInitialisation(ContainerInitialisation containerInitialisation)
        {
            foreach (var element in containerInitialisation.Elements)
            {
                element.Accept(this);
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

            var targets = loopControl.Token.TokenType == TType.Break ? _breakTargets : _continueTargets;
            int handlersToLeave = targets.Count > 0 ? _tryDepth - targets.Peek() : 0;

            Emit(new LoopControl(0, loopControl.Token.TokenType, true, handlersToLeave));
        }

        public void VisitConstructor(Constructor constructor)
        {
            foreach (var parameter in constructor.Parameters)
            {
                parameter.Accept(this);
            }

            Emit(WithArgumentInfo(new NewObject(constructor.ClassName, constructor.Parameters.Count, true) { ClrType = constructor.ClrType }, constructor));
        }

        public void VisitAssignment(Assignment assignment)
        {
            EmitDebugSymbol(assignment);

            assignment.Expression.Accept(this);

            if (assignment.Assignee is FieldByIdAccess fieldById)
            {
                fieldById.Caller.Accept(this);
                fieldById.FieldId.Accept(this);
                Emit(new Store(fieldById.Name, true, false, false, null, null) { IsFieldById = true });
                return;
            }

            if (assignment.Assignee.StaticCall)
            {
                Emit(new StaticStore(assignment.Assignee.Name, ((Word)assignment.Assignee.Caller.Token).Lexeme)
                {
                    ClrType = (assignment.Assignee.Caller as Variable)?.ClrType
                });
                return;
            }

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

            Emit(new Store(assignment.Assignee.Name, fromCaller, false, isArray, null, null));
        }

        public void VisitContainerAssignment(ContainerAssignment containerAssignment)
        {
            EmitDebugSymbol(containerAssignment);

            containerAssignment.Expression.Accept(this);

            int containerIndex = 1;
            int maxIndex = containerAssignment.Assignees.Count;

            foreach (var assignee in containerAssignment.Assignees)
            {
                bool fromCaller = assignee.Caller != null;
                if (fromCaller)
                {
                    assignee.Caller.Accept(this);
                }

                bool isArray = false;
                if (assignee is ArrayAccess access)
                {
                    isArray = true;
                    access.Index.Accept(this);
                }

                var fieldById = assignee as FieldByIdAccess;

                fieldById?.FieldId.Accept(this);

                Emit(new ContainerStore(assignee.Name, containerIndex, containerIndex == maxIndex, fromCaller, false, isArray, null, null) { IsFieldById = fieldById != null });

                containerIndex++;
            }
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
                Emit(new ToBooleanValue());
                var scope = ReleaseScope();

                if (tokenType == TType.And)
                {
                    Emit(new JumpIfFalse(scope.Count + 1));
                }
                else
                {
                    Emit(new JumpIfTrue(scope.Count + 1));
                }

                EmitScope(scope);
            }
        }

        public void VisitBlock(Block block)
        {
            EmitDebugSymbol(block);
            Emit(new BeginScope());

            CreateScope();
            foreach (var statement in block.Statements)
            {
                statement.Accept(this);
            }
            var blockScope = ReleaseScope();

            EmitScope(blockScope);
            Emit(new EndScope());
        }

        public void VisitConstant(Constant constant)
        {
            object value = null;

            if (constant.Token is BaseType baseType)
            {
                value = baseType.Value;
            }
            else if (constant.Token.TokenType == TType.True)
            {
                value = true;
            }
            else if (constant.Token.TokenType == TType.False)
            {
                value = false;
            }

            Emit(new Push(value));
        }

        public void VisitDo(Do @do)
        {
            EmitDebugSymbol(@do);

            BeginLoop();
            CreateScope();
            @do.Block.Accept(this);
            var blockScope = ReleaseScope();
            EndLoop();

            CreateScope();
            @do.Expression.Accept(this);
            var expressionScope = ReleaseScope();

            CreateScope();
            if (_hasMaxIterations)
            {
                Emit(new MaxLoopThrow());
            }

            var loopCheckScope = ReleaseScope();

            if (_hasMaxIterations)
            {
                Emit(new Push(Options.MaxLoopIterations));
            }

            EmitScope(loopCheckScope);
            EmitScope(blockScope);
            EmitScope(expressionScope);
            Emit(new JumpIfTrue(-(loopCheckScope.Count + blockScope.Count + expressionScope.Count), false));

            RecalculateLoopControlOffsets(expressionScope.Instructions.First());
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

            BeginLoop();
            CreateScope();
            @for.Block.Accept(this);
            var blockScope = ReleaseScope();
            EndLoop();

            CreateScope();
            @for.LoopStatement.Accept(this);
            var loopScope = ReleaseScope();

            CreateScope();
            if (_hasMaxIterations)
            {
                Emit(new MaxLoopThrow());
            }

            var maxLoopScope = ReleaseScope();

            if (_hasMaxIterations)
            {
                Emit(new Push(Options.MaxLoopIterations));
            }

            EmitScope(forScope);
            Emit(new JumpIfFalse(maxLoopScope.Count + blockScope.Count + loopScope.Count + 1 + 1 /* Jump instruction */, false));
            EmitScope(maxLoopScope);
            EmitScope(blockScope);
            EmitScope(loopScope);
            Emit(new Jump(-(maxLoopScope.Count + blockScope.Count + loopScope.Count + forScope.Count + 1)));

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
            Emit(new BeginScope());

            foreach (var statement in program.Statements)
            {
                statement.Accept(this);
            }

            Emit(new EndScope());
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
                Emit(new JumpIfFalse(statementScope.Count + 2, false));
            }
            else
            {
                Emit(new JumpIfFalse(statementScope.Count + 1, false));
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

            BeginLoop();
            CreateScope();
            @while.Block.Accept(this);
            var blockScope = ReleaseScope();
            EndLoop();

            CreateScope();
            if (_hasMaxIterations)
            {
                Emit(new MaxLoopThrow());
            }

            var loopCheckScope = ReleaseScope();

            if (_hasMaxIterations)
            {
                Emit(new Push(Options.MaxLoopIterations));
            }

            EmitScope(expressionScope);
            Emit(new JumpIfFalse(loopCheckScope.Count + blockScope.Count + 1 + 1 /*Jump instruction*/, false));
            EmitScope(loopCheckScope);
            EmitScope(blockScope);
            Emit(new Jump(-(loopCheckScope.Count + blockScope.Count + expressionScope.Count) - 1));

            RecalculateLoopControlOffsets(expressionScope.Instructions.First());
        }

        private void BeginLoop()
        {
            _breakTargets.Push(_tryDepth);
            _continueTargets.Push(_tryDepth);
        }

        private void EndLoop()
        {
            _breakTargets.Pop();
            _continueTargets.Pop();
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
            bool isInstanceCall = functionCall.Caller != null && !functionCall.StaticCall;

            // Evaluation order is the one of the X++ compiler: the caller, then the arguments from left to right
            if (isInstanceCall)
            {
                functionCall.Caller.Accept(this);
            }

            foreach (var parameter in functionCall.Parameters)
            {
                parameter.Accept(this);
            }

            int nArgs = functionCall.Parameters.Count;

            if (_declaredFunctions.Exists(f => f.Declaration.Name.ToLowerInvariant() == functionCall.Name.ToLowerInvariant()) && functionCall.Caller is null)
            {
                var declaredFunctionRef = _declaredFunctions.First(f => f.Declaration.Name.ToLowerInvariant() == functionCall.Name.ToLowerInvariant());
                Emit(new DeclaredFunctionCall(declaredFunctionRef, nArgs, allocate));
            }
            else if (functionCall.Intrinsical)
            {
                Emit(WithArgumentInfo(new IntrinsicCall(functionCall.Name, nArgs, allocate), functionCall));
            }
            else if (functionCall.Caller is null)
            {
                Emit(WithArgumentInfo(new StaticFunctionCall(functionCall.Name, nArgs, allocate), functionCall));
            }
            else if (functionCall.StaticCall)
            {
                var caller = functionCall.Caller as Variable;
                string callerName = ((Word)caller.Token).Lexeme;
                Emit(WithArgumentInfo(new StaticFunctionCall(functionCall.Name, nArgs, allocate, callerName) { ClrType = caller.ClrType }, functionCall));
            }
            else
            {
                Emit(WithArgumentInfo(new InstanceFunctionCall(functionCall.Name, nArgs, allocate), functionCall));
            }
        }

        private static Call WithArgumentInfo(Call call, FunctionCall functionCall)
        {
            call.ArgumentTypes = functionCall.ArgumentTypes;

            var byRefArguments = functionCall.Parameters
                .Select((parameter, position) => new { Variable = parameter as Variable, Position = position })
                .Where(p => p.Variable != null && p.Variable.IsByRef)
                .Select(p => new KeyValuePair<int, string>(p.Position, p.Variable.Name))
                .ToList();

            if (byRefArguments.Count > 0)
            {
                call.ByRefArguments = byRefArguments;
            }

            return call;
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

            Emit(new JumpIfFalse(leftScope.Count + 1 + 1 /*Jump instruction */, false));
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

                case TType.BinaryNot:
                    Emit(new Push(-1));
                    Emit(new Arithmetic(TType.BinaryXOr));
                    break;
            }
        }

        public void VisitVariable(Variable variable)
        {
            if (variable.IsSystemVariable)
            {
                Emit(new SystemVariableLoad(variable.Name));
                return;
            }

            if (variable is FieldByIdAccess fieldById)
            {
                fieldById.Caller.Accept(this);
                fieldById.FieldId.Accept(this);
                Emit(new FieldByIdLoad());
                return;
            }

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
                Emit(new StaticLoad(variable.Name, callerName, isArray) { ClrType = (variable.Caller as Variable)?.ClrType });
            }
        }

        public void VisitVariableDeclarations(VariableDeclarations variableDeclarations)
        {
            // An array is part of the statement that declares it: one step for the debugger
            if (!(variableDeclarations is VariableArrayDeclaration))
            {
                EmitDebugSymbol(variableDeclarations);
            }


            foreach (var arrayDeclaration in variableDeclarations.ArrayDeclarations)
            {
                arrayDeclaration.Accept(this);
            }

            foreach (var declaration in variableDeclarations.Identifiers)
            {
                if (declaration.Value != null)
                {
                    declaration.Value.Accept(this);
                }
                else if (variableDeclarations is VariableArrayDeclaration arrayDeclaration)
                {
                    arrayDeclaration.Size?.Accept(this);
                    Emit(new DefaultValueArray(variableDeclarations.DeclarationType.Lexeme, arrayDeclaration.Size != null));
                }
                else if (variableDeclarations.IsClrType)
                {
                    Emit(new ClrDefaultValue(variableDeclarations.DeclarationClrType));
                }
                else
                {
                    Emit(new DefaultValue(variableDeclarations.DeclarationType.Lexeme));
                }

                Emit(new Store(declaration.Key.Lexeme, false, true, false, variableDeclarations.IsClrType ? null : variableDeclarations.DeclarationType.Lexeme, variableDeclarations.DeclarationClrType)
                {
                    StringLength = variableDeclarations.StringLength
                });
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

            var cases = new List<System.Tuple<List<ByteCodeGenerationScope>, ByteCodeGenerationScope>>();

            _breakTargets.Push(_tryDepth);

            // Emit cases
            foreach (var @case in @switch.Cases)
            {
                var values = new List<ByteCodeGenerationScope>();

                CreateScope();
                EmitDebugSymbol(@case.Key);
                @case.Key.Accept(this);
                // delete all debug symbols that are created by the expression itself
                DeleteAllDebugSymbolsInScope(true);
                values.Add(ReleaseScope());

                if (@switch.ExtraCaseValues.TryGetValue(@case.Key, out var extraValues))
                {
                    foreach (var extraValue in extraValues)
                    {
                        CreateScope();
                        extraValue.Accept(this);
                        DeleteAllDebugSymbolsInScope(false);
                        values.Add(ReleaseScope());
                    }
                }

                CreateScope();
                foreach (var caseStatement in @case.Value)
                {
                    caseStatement.Accept(this);
                }
                var caseStatementsScope = ReleaseScope();

                cases.Add(new System.Tuple<List<ByteCodeGenerationScope>, ByteCodeGenerationScope>(values, caseStatementsScope));
            }

            // Emit default
            CreateScope();
            foreach (var defaultStatement in @switch.Default)
            {
                defaultStatement.Accept(this);
            }
            var defaultScope = ReleaseScope();

            _breakTargets.Pop();

            Emit(new BeginScope());

            EmitScope(expressionScope);

            for (int nCase = 0; nCase < cases.Count; nCase++)
            {
                var values = cases[nCase].Item1;
                var statements = cases[nCase].Item2;

                // A case without break runs into the statements of the next one, without testing its values
                int nextTestsCount = nCase + 1 < cases.Count ? cases[nCase + 1].Item1.Sum(v => v.Count + 1) : 0;

                for (int nValue = 0; nValue < values.Count - 1; nValue++)
                {
                    int remainingTests = values.Skip(nValue + 1).Sum(v => v.Count + 1);

                    EmitScope(values[nValue]);
                    Emit(new CaseMatch(remainingTests + 1));
                }

                EmitScope(values.Last());
                Emit(new Case(statements.Count + (nextTestsCount > 0 ? 1 : 0) + 1));
                EmitScope(statements);

                if (nextTestsCount > 0)
                {
                    Emit(new Jump(nextTestsCount + 1));
                }
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

                if (skipFirst && total == 0)
                {
                    break;
                }

                _ss.Peek().Instructions.Remove(instruction);
            }
        }

        public void VisitFunctionDeclaration(FunctionDeclaration functionDeclaration)
        {
            // Create dummy function
            RefFunction refFunction = new RefFunction(functionDeclaration);
            _declaredFunctions.Add(refFunction);

            // The function runs with its own exception handlers
            int enclosingTryDepth = _tryDepth;
            _tryDepth = 0;

            CreateScope();

            // A default value can use the previous parameters, so it runs inside the function
            foreach (var parameter in functionDeclaration.Parameters.Where(p => p.DefaultValue != null))
            {
                CreateScope();
                parameter.DefaultValue.Accept(this);
                Emit(new Store(parameter.Name, false, true, false, null, parameter.DeclarationClrType));
                var defaultValueScope = ReleaseScope();

                Emit(new JumpIfParameterPassed(parameter.Name, defaultValueScope.Count + 1));
                EmitScope(defaultValueScope);
            }

            functionDeclaration.Block.Accept(this);
            var blockScope = ReleaseScope();

            _tryDepth = enclosingTryDepth;

            refFunction.Instructions = blockScope.Instructions;
        }

        public void VisitReturn(Parser.Return @return)
        {
            EmitDebugSymbol(@return);
            @return.Expression?.Accept(this);
            Emit(new Return(@return.Expression != null));
        }

        public void VisitPrint(Print print)
        {
            EmitDebugSymbol(print);

            // Print call is translated into strFmt and info calls
            string format = string.Join(" ", Enumerable.Range(1, print.Parameters.Count).Select(n => $"%{n}"));

            Emit(new Push(format));

            foreach (var parameter in print.Parameters)
            {
                parameter.Accept(this);
            }

            Emit(new StaticFunctionCall("strFmt", print.Parameters.Count + 1, true));
            Emit(new StaticFunctionCall("info", 1, false));
        }

        public void VisitIs(Parser.Is @is)
        {
            @is.Expression.Accept(this);
            Emit(new Is(@is.TypeName) { ClrType = @is.ClrType });
        }

        public void VisitAs(Parser.As @as)
        {
            @as.Expression.Accept(this);
            Emit(new As(@as.TypeName) { ClrType = @as.ClrType, IsMapCall = @as.IsMapCall });
        }

        public void VisitTry(Try @try)
        {
            EmitTry(@try);
        }

        private void EmitTry(Try @try, IInstruction finallyInstruction = null)
        {
            var catchBlockScopes = new List<System.Tuple<Catch, ByteCodeGenerationScope>>(@try.Catches.Count);

            // The handler stays active during the try and the catch blocks
            _tryDepth++;

            int totalCatchInstructions = 0;
            foreach (var @catch in @try.Catches)
            {
                CreateScope();
                @catch.Block.Accept(this);
                ByteCodeGenerationScope catchScope = ReleaseScope();
                catchBlockScopes.Add(new System.Tuple<Catch, ByteCodeGenerationScope>(@catch, catchScope));
                totalCatchInstructions += catchScope.Count;
            }

            int tryOffset = totalCatchInstructions + catchBlockScopes.Count + 1 /* Jump instruction */;
            CreateScope();
            @try.TryBlock.Accept(this);
            var tryScope = ReleaseScope();

            var handler = new BeginHandleException(tryOffset + tryScope.Count + 1);
            Emit(handler);
            EmitScope(tryScope);
            Emit(new Jump(tryOffset));

            int accumulatedCatchOffset = tryScope.Count + 1 + 1 /* Jump instruction*/;
            for (int i = 0; i < catchBlockScopes.Count; i++)
            {
                ByteCodeGenerationScope currentCatchScope = catchBlockScopes[i].Item2;

                totalCatchInstructions -= currentCatchScope.Count;
                int offset = totalCatchInstructions + (catchBlockScopes.Count - i) /* Jump instructions */;

                EmitScope(currentCatchScope);
                Emit(new Jump(offset));

                if (i > 0)
                {
                    accumulatedCatchOffset += catchBlockScopes[i - 1].Item2.Count + 1 /* Jump instruction */;
                }

                var @catch = catchBlockScopes[i].Item1;

                handler.CatchReferences.Add(new ExceptionCatchReference(@catch.ExceptionMember, accumulatedCatchOffset)
                {
                    ClrType = @catch.ClrType,
                    VariableName = @catch.VariableName
                });
            }

            _tryDepth--;

            bool emitFinally = @try.HasFinally || finallyInstruction != null;

            Emit(new EndHandleException(emitFinally));

            if (@try.HasFinally)
            {
                @try.Finally.Accept(this);
            }
            else if (finallyInstruction != null)
            {
                Emit(finallyInstruction);
            }

            if (emitFinally)
            {
                Emit(new Finally());
            }
        }

        public void VisitUsing(Using @using)
        {
            /* using (var a = value) 
             * {
             *     ...
             * } 
             * is equial to:
             * var a = value;
             * try
             * {
             *    ...
             * }
             * finally { a.Dispose(); } 
             */

            Emit(new BeginScope());
            @using.VariableDeclaration.Accept(this);

            // Construct placeholder try
            Try @try = new Try(@using.Block, @using.Block.SourceCodeBinding);
            EmitTry(@try, new Dispose(@using.VariableName));
            Emit(new EndScope());
        }

        public void VisitRetry(Parser.Retry retry)
        {
            EmitDebugSymbol(retry);
            Emit(new Retry());
        }

        public void VisitTableField(TableField tableField)
        {

        }

        public void VisitEventHandlerSubscription(EventHandlerSubscription subscription)
        {
            EmitDebugSymbol(subscription);
            subscription.Delegate.Accept(this);
            RefFunction refFunction = _declaredFunctions.First(f => f.Declaration.Name.ToLowerInvariant() == subscription.EventHandler.FunctionName.ToLowerInvariant());
            Emit(new EventHandlerSubscriptionHandle(refFunction));
        }
    }
}
