using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using XppInterpreter.Core;
using XppInterpreter.Lexer;
using XppInterpreter.Parser.Metadata;

namespace XppInterpreter.Parser
{
    public partial class XppParser : IParser
    {
        private Token currentToken;

        private IScanResult lastScanResult;
        private IScanResult currentScanResult;
        private bool hasBeenInitialized = false;
        private int currentPeekOffset = -1;
        private readonly ILexer _lexer;
        private readonly ParseContext _parseContext = new ParseContext();
        private readonly Interpreter.Proxy.XppProxy _proxy;
        private readonly ParseErrorCollection _parseErrors = new ParseErrorCollection();

        private bool _forAutoCompletion;
        private bool _forMetadata;
        private AutoCompletionPurpose _autoCompletionPurpose;
        private int _stopAtRow, _stopAtColumn;
        private XppTypeInferer _typeInferer = null;
        IScanResult AdvancePeek(bool reset = false)
        {
            currentPeekOffset++;
            var ret = _lexer.Peek(currentPeekOffset);
            if (reset)
            {
                ResetPeek();
            }
            return ret;
        }

        void ResetPeek()
        {
            currentPeekOffset = -1;
        }

        public XppParser(ILexer lexer, Interpreter.Proxy.XppProxy xppProxy)
        {
            _lexer = lexer;
            _proxy = xppProxy;
        }

        public ParseResult Parse()
        {
            Initialize();

            Program program = null;

            try
            {
                program = Program();
            }
            catch (ParseException ex)
            {
                _parseErrors.Add(new ParseError(ex.Token, ex.Line, ex.Position, ex.Message));
            }

            return new ParseResult(program, _parseErrors);
        }

        public TokenMetadata GetTokenMetadataAt(int row, int column, bool isMethodParameters)
        {
            _forMetadata = true;
            _stopAtRow = row;
            _stopAtColumn = column;

            try
            {
                Parse();
            }
            catch (MetadataInterruption interruption)
            {
                return interruption.TokenData;
            }
            catch
            {
                return null;
            }

            return null;
        }

        public System.Type ParseForAutoCompletion(int row, int column, AutoCompletionPurpose purpose)
        {
            _forAutoCompletion = true;
            _stopAtRow = row;
            _stopAtColumn = column;
            _autoCompletionPurpose = purpose;

            try
            {
                Parse();
            }
            catch (AutoCompletionTypeInterruption interruption)
            {
                return interruption.InferedType;
            }
            catch
            {
                return null;
            }

            return null;
        }

        internal Program Program()
        {
            var startResult = currentScanResult;
            var stmts = new List<Statement>();

            while (currentToken.TokenType != TType.EOF)
            {
                stmts.Add(Statement());
            }

            return new Program(stmts, SourceCodeBinding(startResult, lastScanResult ?? startResult));
        }

        internal Block Block()
        {
            _parseContext.BeginScope();

            var start = Match(TType.LeftBrace);
            var stmts = new List<Statement>();

            while (currentToken.TokenType != TType.RightBrace && currentToken.TokenType != TType.EOF)
            {
                stmts.Add(Statement());
            }

            var end = Match(TType.RightBrace);

            _parseContext.EndScope();

            return new Block(stmts, SourceCodeBinding(start, end), DebuggeableBinding(start));
        }

        internal Constructor Constructor()
        {
            var start = currentScanResult;

            Match(TType.New);
            Word identifier = (Word)Match(TType.Id).Token;

            return new Constructor(
                identifier,
                Parameters(null, identifier.Lexeme, false, true),
                null,
                false,
                SourceCodeBinding(start, lastScanResult),
                SourceCodeBinding(start, lastScanResult));
        }

        internal List<string> IntrinsicParameters(string methodName)
        {
            List<string> literalParameters = new List<string>();
            var start = Match(TType.LeftParenthesis);

            int parameterCount = 0;

            HandleMetadataInterruption(start.Line, start.Start, start.Start + 1, null, methodName, parameterCount, true, false, false);

            while (currentToken.TokenType != TType.RightParenthesis)
            {
                var parameter = MatchMultiple(TType.Id, TType.String).Token;

                string literalValue = string.Empty;

                if (parameter is Word word)
                {
                    literalValue = word.Lexeme;
                }
                else if (parameter is Lexer.String str)
                {
                    literalValue = (string)str.Value;
                }

                literalParameters.Add(literalValue);

                if (currentToken.TokenType != TType.RightParenthesis)
                {
                    var comma = Match(TType.Comma);
                    parameterCount ++;
                    HandleMetadataInterruption(comma.Line, comma.Start, comma.Start + 1, null, methodName, parameterCount, true, false, false);
                }
            }

            Match(TType.RightParenthesis);

            return literalParameters;
        }

        internal List<Expression> Parameters(Expression caller, string tokenName, bool isStatic, bool isConstructor)
        {
            List<Expression> parameters = new List<Expression>();

            IScanResult start = Match(TType.LeftParenthesis);

            int parameterCount = 0;

            HandleMetadataInterruption(start.Line, start.Start, start.Start + 1, caller, tokenName, parameterCount, false, isStatic, isConstructor);

            while (currentToken.TokenType != TType.RightParenthesis)
            {
                parameters.Add(Expression());

                if (currentToken.TokenType != TType.RightParenthesis)
                {
                    var comma = Match(TType.Comma);

                    parameterCount++;
                    HandleMetadataInterruption(comma.Line, comma.Start, comma.Start + 1, caller, tokenName, parameterCount, false, isStatic, isConstructor);
                }
            }

            Match(TType.RightParenthesis);

            return parameters;
        }

        internal Expression IntrinsicFunction(string functionName)
        {
            var start = lastScanResult;
            HandleMetadataInterruption(start.Line, start.Start, start.End, start.Token, TokenMetadataType.IntrinsicMethod);

            var parameters = IntrinsicParameters(functionName);
            object result;
            Expression ret = null;

            try
            {
                if (!_forAutoCompletion && !_forMetadata)
                { 
                    // Call intrinsic function
                    result = Interpreter.Proxy.XppProxyHelper.CallIntrinsicFunction(_proxy.Intrinsic, functionName, parameters.ToArray<object>());
                }
                else
                {
                    result = -1;
                }
            }
            catch (Exception ex)
            {
                result = -1;
                string message = ex.Message;
                if (ex.InnerException != null)
                {
                    message = ex.InnerException.Message;
                }

                HandleParseError(message, false, false);
            }

            var binding = SourceCodeBinding(start, lastScanResult);

            if (result is int intValue)
            {
                ret = new Constant(intValue, binding);
            }
            else if (result is string strValue)
            {
                ret = new Constant(strValue, binding);
            }
            else if (result is object[] conValue)
            {
                ret = new Constant(conValue, binding);
            }

            return ret;
        }

        internal Expression Variable(Expression caller = null, bool staticCall = false, bool expectReturn = true)
        {
            IScanResult identifier = caller is null ? Match(TType.Id) : MatchAnyWord();

            Expression ret;
            SourceCodeBinding debuggeableStartBinding = caller?.SourceCodeBinding ?? new SourceCodeBinding(identifier.Line, identifier.Start, 0, 0);
            bool validateVariableName = false;

            if (caller is Interpreter.Debug.IDebuggeable debuggeable)
            {
                debuggeable.DebuggeableBinding = null;
            }

            if (currentToken.TokenType == TType.LeftParenthesis)
            {
                bool isInsideFunctionScope = _parseContext.CallFunctionScope.Empty;

                _parseContext.CallFunctionScope.New();

                string functionName = (identifier.Token as Word).Lexeme;
                bool intrinsical = caller is null && Interpreter.Proxy.XppProxyHelper.IsIntrinsicFunction(functionName);
                if (intrinsical)
                {
                    ret = IntrinsicFunction(functionName);
                }
                else
                {
                    var funcName = (Word)identifier.Token;

                    HandleMetadataInterruption(
                        identifier.Line, 
                        identifier.Start, 
                        identifier.End, 
                        identifier.Token,
                        staticCall ? TokenMetadataType.StaticMethod : 
                            caller is null ? TokenMetadataType.GlobalOrDefinedMethod : 
                                             TokenMetadataType.InstanceMethod,
                        caller);

                    var parameters = Parameters(caller, funcName.Lexeme, staticCall, false);

                    ret = new FunctionCall(
                        funcName,
                        parameters,
                        caller,
                        staticCall,
                        false,
                        SourceCodeBinding(identifier, lastScanResult),
                        isInsideFunctionScope ? SourceCodeBinding(debuggeableStartBinding, lastScanResult) : null);
                }

                _parseContext.CallFunctionScope.Release();
            }
            else
            {
                validateVariableName = caller is null;

                HandleMetadataInterruption(
                    identifier.Line,
                    identifier.Start,
                    identifier.End,
                    identifier.Token,
                    TokenMetadataType.Variable,
                    caller);

                if (currentToken.TokenType == TType.LeftBracket)
                {
                    Match(TType.LeftBracket);
                    Expression index = Expression();
                    Match(TType.RightBracket);

                    ret = new ArrayAccess(
                        (Word)identifier.Token,
                        caller,
                        index,
                        staticCall,
                        SourceCodeBinding(identifier, lastScanResult));
                }
                else
                {
                    ret = new Variable(
                        (Word)identifier.Token,
                        caller,
                        staticCall,
                        SourceCodeBinding(identifier, lastScanResult));
                }
            }

            // Static calls cannot be from instances, so skip validation
            if (currentToken.TokenType != TType.StaticDoubleDot && validateVariableName)
            {
                string variableName = ((Word)identifier.Token).Lexeme;
                var declaredVariable = _parseContext.CurrentScope.FindVariableDeclaration(variableName);

                if (declaredVariable is null)
                {
                    HandleParseError(string.Format(MessageProvider.ExceptionVariableNotDeclared, variableName), stop: false);
                }
            }

            System.Type returnType = _typeInferer.InferType(ret, currentToken.TokenType == TType.StaticDoubleDot, _parseContext);

            if (ret is FunctionCall && expectReturn && (returnType is null || returnType == typeof(void)))
            {
                HandleParseError(MessageProvider.ExceptionTypeCannotBeNull, stop: false);
            }

            if (ret is Variable variable && variable.Caller != null)
            {
                var callerType = _typeInferer.InferType(variable.Caller, variable.StaticCall, _parseContext);
                ValidateVariableMember(callerType, ret);
            }

            if (currentToken.TokenType == TType.Dot || currentToken.TokenType == TType.StaticDoubleDot)
            {
                if (currentToken.TokenType == TType.StaticDoubleDot &&
                    (ret is FunctionCall || ret is ArrayAccess))
                {
                    HandleParseError(string.Format(MessageProvider.ExceptionTokenWasNotExpected, currentToken.TokenType));
                }

                MatchMultiple(TType.Dot, TType.StaticDoubleDot);
                HandleAutocompletion(ret);

                ret = Variable(ret, lastScanResult.Token.TokenType == TType.StaticDoubleDot, expectReturn: expectReturn);
            }

            return ret;
        }

        private void ValidateVariableMember(System.Type callerType, Expression expression)
        {
            if (callerType is null) return;

            if (callerType.IsEnum)
            {
                if (expression is FunctionCall)
                {
                    HandleParseError(MessageProvider.ExceptionCannotCallEnumMethod, stop: false);
                }
                else if (expression is Variable variable && !_proxy.Reflection.EnumHasMember(callerType.Name, variable.Name))
                {
                    HandleParseError(string.Format(MessageProvider.ExceptionInvalidEnumMember, callerType.Name, variable.Name), stop: false);
                }

                return;
            }
            else if (expression is FunctionCall functionCall)
            {
                if (!_proxy.Reflection.TypeHasMethod(callerType, functionCall.Name))
                {
                    HandleParseError(string.Format(MessageProvider.ExceptionInvalidTypeMethod, callerType.Name, functionCall.Name), stop: false);
                }
            }
            else if (expression is Variable variable)
            {
                if (!_proxy.Reflection.TypeHasProperty(callerType, variable.Name))
                {
                    HandleParseError(string.Format(MessageProvider.ExceptionInvalidTypeField, callerType.Name, variable.Name), stop: false);
                }
            }
        }

        internal List<Statement> Default()
        {
            Match(TType.Default);
            var statements = new List<Statement>();
            Match(TType.DoubleDot);

            while (currentToken.TokenType != TType.RightBrace)
            {
                statements.Add(Statement());
            }

            return statements;
        }

        internal KeyValuePair<Expression, List<Statement>> Case()
        {
            var caseResult = Match(TType.Case);
            var expression = Expression();
            var statements = new List<Statement>();

            var caseResultEnd = Match(TType.DoubleDot);
            while (currentToken.TokenType != TType.Case &&
                   currentToken.TokenType != TType.Default &&
                   currentToken.TokenType != TType.RightBrace)
            {
                statements.Add(Statement());
            }

            expression.DebuggeableBinding = SourceCodeBinding(caseResult, caseResultEnd);

            return new KeyValuePair<Expression, List<Statement>>(expression, statements);
        }

        internal Switch Switch()
        {
            var switchResult = Match(TType.Switch);
            Match(TType.LeftParenthesis);
            var expression = Expression();
            var switchResultEnd = Match(TType.RightParenthesis);
            Match(TType.LeftBrace);

            List<Statement> defaultStatements = null;
            IDictionary<Expression, List<Statement>> cases = null;

            while (currentToken.TokenType == TType.Case || currentToken.TokenType == TType.Default)
            {
                if (defaultStatements != null)
                {
                    if (currentToken.TokenType == TType.Case)
                    {
                        HandleParseError(MessageProvider.ExceptionDefaultSwitchStmt, stop: false);
                    }
                    else if (currentToken.TokenType == TType.Default)
                    {
                        HandleParseError(MessageProvider.ExceptionMultiDefaultSwitchStmt, stop: false);
                    }
                }

                if (currentToken.TokenType == TType.Case)
                {
                    if (cases is null)
                    {
                        cases = new Dictionary<Expression, List<Statement>>();
                    }

                    cases.Add(Case());
                }
                else if (currentToken.TokenType == TType.Default)
                {
                    defaultStatements = Default();
                }
            }

            Match(TType.RightBrace);

            return new Switch(
                expression,
                cases,
                defaultStatements,
                SourceCodeBinding(switchResult, lastScanResult),
                SourceCodeBinding(switchResult, switchResultEnd));
        }

        internal If If(IScanResult elseScan = null)
        {
            var ifResult = Match(TType.If);
            var debuggeableStartResult = elseScan is null ? ifResult : elseScan;

            Match(TType.LeftParenthesis);
            Expression expression = Expression();

            var endIfResult = Match(TType.RightParenthesis);

            Block block = Block();

            If @else = null;

            if (currentToken.TokenType == TType.Else)
            {
                var elseResult = Match(TType.Else);

                if (currentToken.TokenType == TType.If)
                {
                    @else = If(elseResult);
                }
                else
                {
                    @else = new Else(
                        Statement(),
                        SourceCodeBinding(elseResult, lastScanResult),
                        DebuggeableBinding(elseResult));
                }
            }

            return new If(
                expression,
                block,
                @else,
                SourceCodeBinding(ifResult, block.SourceCodeBinding),
                SourceCodeBinding(debuggeableStartResult, endIfResult));
        }

        internal While While()
        {
            var bindingStart = Match(TType.While);

            Match(TType.LeftParenthesis);
            Expression condition = Expression();

            var bindingEnds = Match(TType.RightParenthesis);

            using (new LoopControlContext(_parseContext))
            { 
                return new While(condition, Block(),
                    SourceCodeBinding(bindingStart, lastScanResult),
                    SourceCodeBinding(bindingStart, bindingEnds));
            }

        }

        internal For For()
        {
            For @for;
            var start = Match(TType.For);
            Match(TType.LeftParenthesis);

            _parseContext.BeginScope();

            Statement initialisation = Statement(false);
            Match(TType.Semicolon);
            Expression expression = Expression();
            Match(TType.Semicolon);
            Statement loopStmt = Statement(false);
            var end = Match(TType.RightParenthesis);

            using (new LoopControlContext(_parseContext))
            { 
                @for = new For(initialisation, expression, loopStmt, Block(), 
                    SourceCodeBinding(start, lastScanResult), 
                    SourceCodeBinding(start, end));
            }

            _parseContext.EndScope();

            return @for;
        }

        internal Do Do()
        {
            var start = Match(TType.Do);

            using (new LoopControlContext(_parseContext))
            { 
                Block block = Block();
                Match(TType.While);
                Match(TType.LeftParenthesis);
                Expression expression = Expression();
                Match(TType.RightParenthesis);
                Match(TType.Semicolon);

                return new Do(expression, block, 
                    SourceCodeBinding(start, lastScanResult), 
                    DebuggeableBinding(start));
            }
        }

        internal VariableDeclarations VariableDeclaration(bool matchSemicolon = true, bool isUsing = false)
        {
            Dictionary<Word, Expression> declarations = new Dictionary<Word, Expression>();
            Word arrayIdentifier = null;
            Expression arraySize = null;

            var typeResult = MatchMultiple(
                TType.Id,
                TType.TypeAnytype,
                TType.TypeBoolean,
                TType.TypeContainer,
                TType.TypeInt32,
                TType.TypeInt64,
                TType.TypeGuid,
                TType.TypeReal,
                TType.TypeStr,
                TType.TypeTimeOfDay,
                TType.TypeDate,
                TType.TypeDatetime,
                TType.Var);

            Word typeWord = typeResult.Token as Word;

            if (typeWord.TokenType != TType.Var && !_typeInferer.IsKnownType(typeWord.Lexeme))
            {
                HandleParseError(string.Format(MessageProvider.ExceptionTypeNotFound, typeWord.Lexeme), stop: false);
            }

            // Get type from type declaration if it's a known type
            System.Type declarationType = typeWord.TokenType != TType.Var && _typeInferer.IsKnownType(typeWord.Lexeme) ? _proxy.Casting.GetSystemTypeFromTypeName(typeWord.Lexeme) : null;

            bool isArray = false;
            do
            {
                if (isUsing && (declarations.Count > 0 || isArray))
                {
                    HandleParseError(MessageProvider.ExceptionInvalidUsing, stop: false);
                }

                if (declarations.Count > 0 && !isArray)
                {
                    Match(TType.Comma);
                }

                Word id = (Word)Match(TType.Id).Token;
                Expression initialisation = null;

                if (isUsing || currentToken.TokenType == TType.Assign)
                {
                    Match(TType.Assign);
                    initialisation = Expression();
                }
                else if (declarations.Count == 0 && currentToken.TokenType == TType.LeftBracket)
                {
                    Match(TType.LeftBracket);

                    isArray = true;
                    arrayIdentifier = id;

                    if (currentToken.TokenType != TType.RightBracket)
                    {
                        arraySize = Expression();
                    }

                    Match(TType.RightBracket);
                    break;
                }

                declarations[id] = initialisation;

                // Type checking
                if (initialisation != null)
                { 
                    var initializationType = _typeInferer.InferType(initialisation, false, _parseContext);

                    if (declarationType is null)
                    {
                        if (initializationType is null)
                        {
                            HandleParseError(MessageProvider.ExceptionInitializationUnknown, stop: false);
                            declarationType = typeof(object);
                        }
                        else
                        {
                            declarationType = initializationType;
                        }
                    }
                    else if (initializationType != null && !_proxy.Casting.ImplicitConversionExists(initializationType, declarationType))
                    {
                        HandleParseError(string.Format(MessageProvider.ExceptionImplicitConversion, initializationType.Name, declarationType.Name), stop: false);
                    }
                }
            } while (currentToken.TokenType == TType.Comma);

            if (matchSemicolon)
            {
                Match(TType.Semicolon);
            }

            VariableDeclarations ret;

            if (isArray)
            {
                declarationType = _proxy.Casting.GetArrayType(declarationType);

                ret = new VariableArrayDeclaration(typeWord, declarationType, arrayIdentifier, arraySize, SourceCodeBinding(typeResult, lastScanResult));

                _parseContext.CurrentScope.VariableDeclarations.Add(
                    new ParseContextScopeVariable(arrayIdentifier.Lexeme, typeWord, declarationType, true, null));
            }
            else
            {
                ret = new VariableDeclarations(typeWord, declarationType, declarations, SourceCodeBinding(typeResult, lastScanResult));

                foreach (var identifier in ret.Identifiers)
                {
                    _parseContext.CurrentScope.VariableDeclarations.Add(
                        new ParseContextScopeVariable(identifier.Key.Lexeme, ret.DeclarationType, ret.DeclarationClrType, false, identifier.Value));
                }
            }

            return ret;
        }

        internal LoopControl LoopControl()
        {
            if (!_parseContext.CanScapeLoop())
            {
                HandleParseError(MessageProvider.ExceptionLeaveFinally, stop: false);
            }

            var start = currentScanResult;
            Token loopControlToken = currentToken;

            MatchMultiple(TType.Continue, TType.Break);
            Match(TType.Semicolon);

            return new LoopControl(
                loopControlToken,
                SourceCodeBinding(start, lastScanResult));
        }

        internal TtsAbort TtsAbort()
        {
            var start = currentScanResult;

            Match(TType.TtsAbort);
            Match(TType.Semicolon);

            return new TtsAbort(SourceCodeBinding(start, lastScanResult));
        }
        internal TtsCommit TtsCommit()
        {
            var start = currentScanResult;

            Match(TType.TtsCommit);
            Match(TType.Semicolon);

            return new TtsCommit(SourceCodeBinding(start, lastScanResult));
        }

        internal Next Next()
        {
            var start = currentScanResult;
            Match(TType.Next);
            var id = (Word)Match(TType.Id).Token;
            Match(TType.Semicolon);
            return new Next(id.Lexeme, SourceCodeBinding(start, lastScanResult));
        }

        internal Flush Flush()
        {
            var start = currentScanResult;
            Match(TType.Flush);
            var id = (Word)Match(TType.Id).Token;

            if (!_forAutoCompletion && !_forMetadata)
            {
                try
                {
                    _ = _proxy.Intrinsic.tableNum(id.Lexeme);
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message = ex.InnerException.Message;
                    }

                    HandleParseError(message, false, stop: false);
                }
            }

            Match(TType.Semicolon);

            return new Flush(id.Lexeme, SourceCodeBinding(start, lastScanResult));
        }


        internal Throw Throw()
        {
            var start = currentScanResult;

            Match(TType.Throw);
            var exception = Expression();
            Match(TType.Semicolon);

            return new Throw(exception, SourceCodeBinding(start, lastScanResult));
        }

        internal Breakpoint Breakpoint()
        {
            var start = currentScanResult;

            Match(TType.Breakpoint);
            Match(TType.Semicolon);

            return new Breakpoint(SourceCodeBinding(start, lastScanResult));
        }

        internal TtsBegin TtsBegin()
        {
            var start = currentScanResult;

            Match(TType.TtsBegin);
            Match(TType.Semicolon);

            return new TtsBegin(SourceCodeBinding(start, lastScanResult));
        }

        internal Using Using()
        {
            var start = Match(TType.Using);

            _parseContext.BeginScope();

            Match(TType.LeftParenthesis);

            var variable = VariableDeclaration(false);

            System.Type inferedType = _typeInferer.InferType(variable.Identifiers.First().Value, false, _parseContext);

            if (!ReflectionHelper.TypeImplementsInterface(inferedType, typeof(IDisposable)))
            {
                HandleParseError(MessageProvider.ExceptionExpressionIDisposable, stop: false);
            }

            Match(TType.RightParenthesis);

            Using @using = new Using(variable, Block(), SourceCodeBinding(start, lastScanResult));

            _parseContext.EndScope();

            return @using;
        }

        internal Return Return()
        {
            if (_parseContext.FunctionDeclarationStack.Empty)
            {
                HandleParseError(MessageProvider.ExceptionReturnOutOfFunction, stop: false);
            }

            if (_parseContext.WithinFinally())
            {
                HandleParseError(MessageProvider.ExceptionLeaveFinally, stop: false);
            }

            var start = currentScanResult;

            Match(TType.Return);
            var expression = Expression();
            Match(TType.Semicolon);

            return new Return(expression, SourceCodeBinding(start, lastScanResult));
        }

        internal Print Print()
        {
            var start = currentScanResult;

            Match(TType.Print);
            List<Expression> parameters = new List<Expression>();

            do
            {
                if (currentToken.TokenType == TType.Comma)
                {
                    Match(TType.Comma);
                }

                parameters.Add(Expression());
            } while (currentToken.TokenType == TType.Comma);

            Match(TType.Semicolon);

            return new Print(parameters, SourceCodeBinding(start, lastScanResult), SourceCodeBinding(start, lastScanResult));
        }

        internal Retry Retry()
        {
            if (!_parseContext.WithinCatch())
            {
                HandleParseError(MessageProvider.ExceptionRetryNotInCatch, stop: false);
            }

            var start = currentScanResult;

            Match(TType.Retry);
            Match(TType.Semicolon);

            return new Retry(SourceCodeBinding(start, lastScanResult));
        }

        internal Try Try()
        {
            var start = Match(TType.Try);
            var tryBlock = Block();
            var catches = new List<Catch>();

            var tryOrCatch = MatchMultiple(TType.Catch, TType.Finally);

            if (tryOrCatch.Token.TokenType == TType.Catch)
            {
                bool keepParsingCatch;

                do
                {
                    string enumMember = null;
                    if (currentToken.TokenType == TType.LeftParenthesis)
                    {
                        Match(TType.LeftParenthesis);
                        var exceptionTypeExpression = Variable();

                        if (exceptionTypeExpression is FunctionCall || exceptionTypeExpression is ArrayAccess ||
                            (exceptionTypeExpression is Variable v &&
                            (!v.StaticCall || v.Caller is null || (v.Caller as Variable).Caller != null)))
                        {
                            HandleParseError(MessageProvider.ExceptionInvalidCatchExpr, stop: false);
                        }

                        var exceptionTypeVariable = exceptionTypeExpression as Variable;
                        string exceptionEnum = ((Word)(exceptionTypeVariable.Caller as Variable).Token).Lexeme;

                        // Enum must be Exception
                        if (!CatchExceptionTypeHelper.IsExceptionEnum(exceptionEnum))
                        {
                            HandleParseError(MessageProvider.ExceptionInvalidExceptionEnum, stop: false);
                        }

                        enumMember = ((Word)exceptionTypeExpression.Token).Lexeme;
                        if (!CatchExceptionTypeHelper.IsExceptionMember(enumMember))
                        {
                            HandleParseError(string.Format(MessageProvider.ExceptionNotExceptionMember, enumMember), stop: false);
                        }

                        Match(TType.RightParenthesis);
                    }

                    using (var catchContext = new CatchContext(_parseContext))
                    { 
                        catches.Add(new Catch(enumMember, Block()));
                    }

                    if (currentToken.TokenType == TType.Catch)
                    {
                        Match(TType.Catch);
                        keepParsingCatch = true;
                    }
                    else
                    {
                        keepParsingCatch = false;
                    }

                } while (keepParsingCatch);
            }

            Block finallyBlock = null;
            bool parseFinally = currentToken.TokenType == TType.Finally || tryOrCatch.Token.TokenType == TType.Finally;

            if (parseFinally)
            {
                // Match the finally token if not done before
                if (currentToken.TokenType == TType.Finally)
                {
                    Match(TType.Finally);
                }

                using (new LoopControlContext(_parseContext, false))
                { 
                    finallyBlock = Block();
                }
            }

            return new Try(tryBlock,SourceCodeBinding(start, lastScanResult), catches, finallyBlock);
        }

        internal Statement Statement(bool matchSemicolon = true)
        {
            switch (currentToken.TokenType)
            {
                case TType.LeftBracket: return ContainerAssignment();
                case TType.Retry: return Retry();
                case TType.Using: return Using();
                case TType.Try: return Try();
                case TType.Print: return Print();
                case TType.Return: return Return();
                case TType.LeftBrace: return Block();
                case TType.Void:
                case TType.Id:
                    {
                        // Check if the next token is an Id
                        var nextToken = AdvancePeek(false).Token;
                        if (nextToken.TokenType == TType.Id)
                        {
                            nextToken = AdvancePeek(true).Token;
                            if (nextToken.TokenType == TType.LeftParenthesis)
                            {
                                return FunctionDeclaration();
                            }
                            else
                            {
                                return VariableDeclaration(matchSemicolon);
                            }
                        }
                        else
                        {
                            ResetPeek();
                            return Assignment(matchSemicolon);
                        }
                    }
                case TType.Breakpoint: return Breakpoint();
                case TType.Throw: return Throw();
                case TType.TtsBegin: return TtsBegin();
                case TType.TtsAbort: return TtsAbort();
                case TType.TtsCommit: return TtsCommit();
                case TType.If: return If();
                case TType.Next: return Next();
                case TType.Flush: return Flush();
                case TType.InsertRecordset: return InsertRecordset();
                case TType.UpdateRecordset: return UpdateRecordset();
                case TType.DeleteFrom: return DeleteFrom();
                case TType.Select: return SelectStatement();
                case TType.While:
                    {
                        var nextToken = AdvancePeek(true).Token;
                        if (nextToken.TokenType == TType.Select)
                        {
                            return WhileSelect();
                        }
                        else
                        {
                            return While();
                        }
                    }
                case TType.For: return For();
                case TType.Do: return Do();
                case TType.Break:
                case TType.Continue:
                    return LoopControl();
                case TType.Switch: return Switch();
                case TType.ChangeCompany: return ChangeCompany();
                case TType.Unchecked: return Unchecked();
                case TType.Var:
                case TType.TypeStr:
                case TType.TypeDate:
                case TType.TypeDatetime:
                case TType.TypeTimeOfDay:
                case TType.TypeContainer:
                case TType.TypeAnytype:
                case TType.TypeReal:
                case TType.TypeInt32:
                case TType.TypeInt64:
                    {
                        // We advance twice because we want to check for the next token after the Id
                        AdvancePeek(false);
                        var nextToken = AdvancePeek(true).Token;

                        if (nextToken.TokenType == TType.LeftParenthesis)
                        {
                            return FunctionDeclaration();
                        }
                        else
                        {
                            return VariableDeclaration(matchSemicolon);
                        }
                    }
                default:
                    {
                        HandleParseError(MessageProvider.ExceptionInvalidSyntax);
                        return null;
                    }
            }
        }

        internal FunctionDeclaration FunctionDeclaration()
        {
            var start = MatchMultiple(
                TType.Id,
                TType.TypeAnytype,
                TType.TypeBoolean,
                TType.TypeContainer,
                TType.TypeInt32,
                TType.TypeInt64,
                TType.TypeGuid,
                TType.TypeReal,
                TType.TypeStr,
                TType.TypeTimeOfDay,
                TType.TypeDate,
                TType.TypeDatetime,
                TType.Void);

            Word type = start.Token as Word;

            if (type.TokenType != TType.Void && !_typeInferer.IsKnownType(type.Lexeme))
            {
                HandleParseError(string.Format(MessageProvider.ExceptionTypeNotFound, type.Lexeme), stop: false);
            }

            Word funcNameToken = Match(TType.Id).Token as Word;

            Match(TType.LeftParenthesis);

            _parseContext.FunctionDeclarationStack.New();
            _parseContext.CurrentScope.FunctionReferences.Add(new FunctionDeclarationReference((funcNameToken).Lexeme, type));
            _parseContext.CurrentScope.Begin();

            var parameters = new List<FunctionDeclarationParameter>();
            while (currentToken.TokenType != TType.RightParenthesis)
            {
                parameters.Add(FunctionDeclarationParameter());

                if (currentToken.TokenType != TType.RightParenthesis)
                {
                    Match(TType.Comma);
                }
            }

            Match(TType.RightParenthesis);
            var block = Block();

            _parseContext.FunctionDeclarationStack.Release();
            _parseContext.CurrentScope.End();

            var ret = new FunctionDeclaration(
                ((Word)funcNameToken).Lexeme,
                start.Token,
                parameters,
                block,
                SourceCodeBinding(start, lastScanResult));

            _parseContext.CurrentScope.FunctionDeclarations.Add(ret);

            return ret;
        }

        FunctionDeclarationParameter FunctionDeclarationParameter()
        {
            // TODO: allow array types to be function parameters
            var typeResult = MatchMultiple(
                TType.Id,
                TType.TypeAnytype,
                TType.TypeBoolean,
                TType.TypeContainer,
                TType.TypeInt32,
                TType.TypeInt64,
                TType.TypeGuid,
                TType.TypeReal,
                TType.TypeStr,
                TType.TypeTimeOfDay,
                TType.TypeDate,
                TType.TypeDatetime);

            Word type = typeResult.Token as Word;

            if (!_typeInferer.IsKnownType(type.Lexeme))
            {
                HandleParseError(string.Format(MessageProvider.ExceptionTypeNotFound, type.Lexeme), stop: false);
            }

            System.Type inferedType = _proxy.Casting.GetSystemTypeFromTypeName(type.Lexeme);

            Word id = Match(TType.Id).Token as Word;

            _parseContext.CurrentScope.VariableDeclarations.Add(
                new ParseContextScopeVariable(id.Lexeme, type, inferedType, false));

            return new FunctionDeclarationParameter(type, inferedType, id.Lexeme, SourceCodeBinding(typeResult, lastScanResult));
        }

        internal Unchecked Unchecked()
        {
            var start = currentScanResult;

            Match(TType.Unchecked);
            Match(TType.LeftParenthesis);

            Expression expression = Expression();

            var end = Match(TType.RightParenthesis);

            Block block = Block();

            return new Unchecked(expression, block, SourceCodeBinding(start, lastScanResult), SourceCodeBinding(start, end));
        }

        internal ChangeCompany ChangeCompany()
        {
            var start = currentScanResult;

            Match(TType.ChangeCompany);
            Match(TType.LeftParenthesis);

            Expression expression = Expression();

            var end = Match(TType.RightParenthesis);

            Block block = Block();

            return new ChangeCompany(expression, block, SourceCodeBinding(start, lastScanResult), SourceCodeBinding(start, end));
        }

        internal ContainerInitialisation ContainerInitialisation()
        {
            var start = currentScanResult;

            Match(TType.LeftBracket);

            List<Expression> elements = new List<Expression>();

            while (currentToken.TokenType != TType.RightBracket)
            {
                var expression = Expression();

                // Check type
                var elementType = _typeInferer.InferType(expression, _parseContext);

                if (_proxy.Casting.IsReferenceType(elementType))
                {
                    HandleParseError(string.Format(MessageProvider.ExceptionRefTypeContainer, elementType.Name), stop: false);
                }

                elements.Add(expression);

                if (currentToken.TokenType != TType.RightBracket)
                {
                    Match(TType.Comma);
                }
            }

            Match(TType.RightBracket);

            return new ContainerInitialisation(elements, SourceCodeBinding(start, lastScanResult));
        }

        internal Expression Primary()
        {
            Token token = currentToken;

            switch (currentToken.TokenType)
            {
                case TType.LeftBracket:
                    return ContainerInitialisation();

                case TType.LeftParenthesis:
                    Match(TType.LeftParenthesis);
                    Expression node = Expression();
                    Match(TType.RightParenthesis);
                    return node;

                case TType.Plus:
                case TType.Minus:
                    var mpResult = Match(currentToken.TokenType);
                    return new UnaryOperation(mpResult.Token, Expression(), SourceCodeBinding(mpResult, lastScanResult));

                case TType.Negation:
                    var negResult = Match(TType.Negation);
                    return new UnaryOperation(negResult.Token, Primary(), SourceCodeBinding(negResult, lastScanResult));

                case TType.Int32:
                    var integerScan = Match(TType.Int32);
                    return new Constant((int)(integerScan.Token as Lexer.BaseType).Value, SourceCodeBinding(integerScan));

                case TType.Int64:
                    var longScan = Match(TType.Int64);
                    return new Constant((long)(longScan.Token as Lexer.BaseType).Value, SourceCodeBinding(longScan));

                case TType.Real:
                    var doubleScan = Match(TType.Real);
                    return new Constant((decimal)(doubleScan.Token as BaseType).Value, SourceCodeBinding(doubleScan));

                case TType.String:
                    var stringScan = Match(TType.String);
                    HandleMetadataInterruption(stringScan.Line, stringScan.Start, stringScan.End, stringScan.Token, TokenMetadataType.Label);
                    return new Constant((string)(stringScan.Token as BaseType).Value, SourceCodeBinding(stringScan));

                case TType.Date:
                    var dateScan = Match(TType.Date);
                    var date = (DateLiteral)(dateScan.Token as Date).Value;
                    return new Constant(new BaseType(_proxy.Casting.CreateDate(date.Day, date.Month, date.Year), TType.Date), SourceCodeBinding(dateScan));

                case TType.True:
                case TType.False:
                    var boolScan = Match(currentToken.TokenType);
                    return new Constant(boolScan.Token.TokenType == TType.True, SourceCodeBinding(boolScan));

                case TType.Null:
                    var nullScan = Match(TType.Null);
                    return new Constant(Word.Null, SourceCodeBinding(nullScan));

                case TType.Id:
                    return Variable();

                case TType.New:
                    return Constructor();

                default:
                    HandleParseError(string.Format(MessageProvider.ExceptionTokenWasNotExpected, token));
                    return null;
            }
        }

        internal Expression As()
        {
            Expression expr = Primary();

            if (currentToken.TokenType == TType.As)
            {
                Match(TType.As);
                var identifier = Match(TType.Id);

                expr = new As(expr, (identifier.Token as Word).Lexeme, SourceCodeBinding(expr.SourceCodeBinding, identifier));
            }

            return expr;
        }

        internal Expression Factor()
        {
            Expression expr = As();

            while (currentToken.TokenType == TType.Star
                || currentToken.TokenType == TType.Division
                || currentToken.TokenType == TType.Mod
                || currentToken.TokenType == TType.IntegerDivision
                || currentToken.TokenType == TType.LeftShift
                || currentToken.TokenType == TType.RightShift
                || currentToken.TokenType == TType.BinaryAnd
                || currentToken.TokenType == TType.BinaryXOr)
            {
                var result = Match(currentToken.TokenType);
                expr = new BinaryOperation(
                    expr,
                    As(),
                    result.Token,
                    SourceCodeBinding(expr.SourceCodeBinding, lastScanResult));
            }

            return expr;
        }

        internal Expression Bool()
        {
            Expression expr = Equality();

            while (currentToken.TokenType == TType.Or || currentToken.TokenType == TType.And)
            {
                var result = Match(currentToken.TokenType);
                expr = new BinaryOperation(
                    expr,
                    Equality(),
                    result.Token,
                    SourceCodeBinding(expr.SourceCodeBinding, lastScanResult));
            }

            return expr;
        }

        internal Expression Term()
        {
            Expression node = Factor();

            while (currentToken.TokenType == TType.Plus 
                || currentToken.TokenType == TType.Minus
                || currentToken.TokenType == TType.BinaryOr)
            {
                var result = Match(currentToken.TokenType);
                node = new BinaryOperation(
                    node,
                    Factor(),
                    result.Token,
                    SourceCodeBinding(node.SourceCodeBinding, lastScanResult));
            }

            return node;
        }

        internal Expression Comparison()
        {
            Expression expr = Term();

            while (currentToken.TokenType == TType.Greater || currentToken.TokenType == TType.GreaterOrEqual
                || currentToken.TokenType == TType.Smaller || currentToken.TokenType == TType.SmallerOrEqual)
            {
                var result = Match(currentToken.TokenType);
                expr = new BinaryOperation(
                    expr,
                    Term(),
                    result.Token,
                    SourceCodeBinding(expr.SourceCodeBinding, lastScanResult),
                    SourceCodeBinding(expr.SourceCodeBinding, lastScanResult));
            }

            return expr;
        }

        internal Expression Equality()
        {
            var start = currentScanResult;
            Expression expr = Comparison();

            while (currentToken.TokenType == TType.Equal
                || currentToken.TokenType == TType.NotEqual
                || currentToken.TokenType == TType.In
                || currentToken.TokenType == TType.Like
                || currentToken.TokenType == TType.Is)
            {
                if ((currentToken.TokenType == TType.Like
                   || currentToken.TokenType == TType.In)
                    && !isParsingWhereStatement)
                {
                    HandleParseError(MessageProvider.ExceptionInLikeNotInQuery, stop: false);
                }
                else if (currentToken.TokenType == TType.Is &&
                    isParsingWhereStatement)
                {
                    HandleParseError(MessageProvider.ExceptionIsInQuery, stop: false);
                }

                var result = Match(currentToken.TokenType);

                if (lastScanResult.Token.TokenType == TType.Is)
                {
                    var identifier = Match(TType.Id).Token;
                    string typeName = (identifier as Word).Lexeme;
                    expr = new Is(
                        expr, 
                        typeName,
                        SourceCodeBinding(start, lastScanResult));
                }
                else
                { 
                    var binaryExpr = new BinaryOperation(
                        expr,
                        Comparison(),
                        result.Token,
                        SourceCodeBinding(start, lastScanResult));

                    expr = binaryExpr;

                    if (currentToken.TokenType == TType.In &&
                       (binaryExpr.LeftOperand.GetType() != typeof(Variable) ||
                        binaryExpr.RightOperand.GetType() != typeof(Variable)))
                    {
                        HandleParseError(MessageProvider.ExceptionInNotContainer, stop: false);
                    }
                }
            }

            return expr;
        }

        internal Expression Ternary()
        {
            Expression expr = Bool();

            while (currentToken.TokenType == TType.QuestionMark)
            {
                var result = Match(TType.QuestionMark);
                Expression left = Expression();
                Match(TType.DoubleDot);
                Expression right = Expression();

                expr = new Ternary(
                    result.Token,
                    expr,
                    left,
                    right,
                    SourceCodeBinding(result, right.SourceCodeBinding));
            }

            return expr;
        }

        internal Expression Expression()
        {
            Expression expr = Ternary();

            if (currentToken.TokenType == TType.Dot || currentToken.TokenType == TType.StaticDoubleDot)
            {
                MatchMultiple(TType.Dot, TType.StaticDoubleDot);
                expr = Variable(expr);
            }

            return expr;
        }

        internal ContainerAssignment ContainerAssignment()
        {
            var start = currentScanResult;
            Match(TType.LeftBracket);
            List<Variable> assignees = new List<Variable>();
            do
            {
                if (currentToken.TokenType == TType.Comma)
                {
                    Match(TType.Comma);
                }

                Expression assignee = Variable();
                
                if (assignee is FunctionCall || !(assignee is Variable))
                {
                    HandleParseError(MessageProvider.ExceptionInvalidSyntax);
                }

                assignees.Add((Variable)assignee);

            } while (currentToken.TokenType == TType.Comma);

            Match(TType.RightBracket);
            Match(TType.Assign);

            Expression expr = Expression();
            var exprType = _typeInferer.InferType(expr, _parseContext);

            if (exprType != _proxy.Casting.GetSystemTypeFromTypeName("container"))
            {
                HandleParseError(MessageProvider.ExceptionAssignmentNotContainer, stop: false);
            }

            Match(TType.Semicolon);

            return new ContainerAssignment(assignees, expr, SourceCodeBinding(start, lastScanResult));
        }

        internal Statement Assignment(bool matchSemicolon = true)
        {
            var start = currentScanResult;
            Expression assignee = Variable(expectReturn: false);
            Token operand = currentToken;
            Statement ret = null;
            Expression assignedExpression = null;

            switch (currentToken.TokenType)
            {
                case TType.Assign:
                    {

                        Match(TType.Assign);
                        assignedExpression = Expression();
                        ret = new Assignment((Variable)assignee, assignedExpression, SourceCodeBinding(start, lastScanResult));
                    }
                    break;

                case TType.Increment:
                case TType.Decrement:
                    {
                        Match(currentToken.TokenType);
                        assignedExpression = new BinaryOperation(assignee, new Constant(1, null), operand, null);
                        ret = new Assignment((Variable)assignee, assignedExpression , SourceCodeBinding(start, lastScanResult));
                    }
                    break;

                case TType.PlusAssignment:
                case TType.MinusAssignment:
                    {
                        Match(currentToken.TokenType);
                        var binding = SourceCodeBinding(start, currentScanResult);
                        assignedExpression = new BinaryOperation(assignee, Expression(), operand, SourceCodeBinding(start, currentScanResult));
                        ret = new Assignment(
                            (Variable)assignee,
                            assignedExpression,
                            binding);;
                    }
                    break;

                default:
                    {
                        if (assignee is FunctionCall fc)
                        {
                            ret = new NoReturnFunctionCall(fc, SourceCodeBinding(start, lastScanResult));
                        }
                        else
                        {
                            HandleParseError(MessageProvider.ExceptionInvalidSyntax);
                        }
                    }
                    break;
            }

            if (assignee is Variable assigneeVar && assignedExpression != null)
            {
                var assigneeType = _typeInferer.InferType(assignee, false, _parseContext);

                if (assigneeType != null)
                { 
                    var assignedType = _typeInferer.InferType(assignedExpression, false, _parseContext);

                    if (assignedType != null && !_proxy.Casting.ImplicitConversionExists(assignedType, assigneeType))
                    {
                        HandleParseError(string.Format(MessageProvider.ExceptionImplicitConversion, assignedType.Name, assigneeVar.ReturnType.Name), stop: false);
                    }
                }
            }

            if (matchSemicolon)
            {
                var end = Match(TType.Semicolon);

                if (ret.DebuggeableBinding != null)
                {
                    var newBinding = new SourceCodeBinding(
                        ret.DebuggeableBinding.FromLine,
                        ret.DebuggeableBinding.FromPosition,
                        end.Line,
                        end.End);

                    ret.DebuggeableBinding = newBinding;
                }
            }

            return ret;
        }

        void HandleParseError(string s, bool showLine = false, bool stop = true)
        {
            if (stop)
            {
                throw new ParseException(
                    s,
                    currentToken,
                    currentScanResult.Line,
                    currentScanResult.End,
                    showLine);
            }
            else
            {
                _parseErrors.Add(new ParseError(currentToken, currentScanResult.Line, currentScanResult.End, s));
            }
        }

        IScanResult MatchMultiple(params TType[] ttypes)
        {
            if (ttypes.Contains(currentScanResult.Token.TokenType))
            {
                Move();
            }
            else
            {
                HandleParseError(string.Format(MessageProvider.ExceptionTokenWasNotExpected, string.Join(", ", ttypes)));
            }

            // Move function sets the last scan results
            return lastScanResult;
        }

        internal IScanResult MatchAnyWord()
        {
            if (currentToken is Word)
            {
                Move();
            }
            else
            {
                HandleParseError(string.Format(MessageProvider.ExceptionTokenWasNotExpected, TType.Id));
            }

            // Move function sets the last scan results
            return lastScanResult;
        }

        internal IScanResult Match(TType ttype)
        {
            if (currentToken.TokenType == ttype)
            {
                Move();
            }
            else
            {
                HandleParseError(string.Format(MessageProvider.ExceptionTokenWasNotExpected, currentToken.TokenType));
            }

            // Move function sets the last scan results
            return lastScanResult;
        }

        void Initialize()
        {
            if (!hasBeenInitialized)
            {
                _typeInferer = new XppTypeInferer(_proxy);

                hasBeenInitialized = true;
                Move();
            }
        }

        void Move()
        {
            lastScanResult = currentScanResult;
            currentScanResult = _lexer.GetNextToken();
            currentToken = currentScanResult.Token;
        }

        internal void HandleMetadataInterruption(int line, int start, int end, Expression caller, string methodName, int parameterPosition, bool isIntrinsic, bool isStatic, bool isConstructor)
        {
            if (!_forMetadata) return;

            // Search for declaration of the variable
            if (line == _stopAtRow &&
                start <= _stopAtColumn &&
                end >= _stopAtColumn)
            {
                System.Type callerType = null;

                if (caller != null)
                {
                    callerType = _typeInferer.InferType(caller, isStatic, _parseContext);
                }

                throw new MetadataInterruption(TokenMetadataProviderHelper.GetMetadataForMethodParameters(callerType, 
                    methodName, 
                    isIntrinsic, 
                    isStatic, 
                    isConstructor, 
                    parameterPosition,
                    _proxy,
                    _parseContext));
            }
        }

        internal void HandleMetadataInterruption(int line, int start, int end, Token token, TokenMetadataType type, Expression caller = null)
        {
            if (!_forMetadata) return;

            // Search for declaration of the variable
            if (line == _stopAtRow &&
                start <= _stopAtColumn &&
                end >= _stopAtColumn)
            {
                if (type == TokenMetadataType.IntrinsicMethod ||
                    type == TokenMetadataType.GlobalOrDefinedMethod ||
                    type == TokenMetadataType.StaticMethod ||
                    type == TokenMetadataType.InstanceMethod ||
                    type == TokenMetadataType.Constructor)
                {
                    var methodName = (token as Word).Lexeme;
                    System.Type callerType = null;

                    if (caller != null)
                    {
                        callerType = _typeInferer.InferType(caller, type == TokenMetadataType.StaticMethod, _parseContext);

                        if (callerType is null) throw new MetadataInterruption(null);
                    }

                    throw new MetadataInterruption(TokenMetadataProviderHelper.GetMethodMetadata(
                        callerType, 
                        methodName,
                        type == TokenMetadataType.IntrinsicMethod,
                        type == TokenMetadataType.StaticMethod,
                        type == TokenMetadataType.Constructor,
                        _proxy, 
                        _parseContext));
                }
                else if (type == TokenMetadataType.Label)
                {
                    throw new MetadataInterruption(TokenMetadataProviderHelper.GetLabelMetadata((token as Lexer.String).Value.ToString(), _proxy));
                }
                else if (type == TokenMetadataType.Variable)
                {
                    var varName = (token as Word).Lexeme;

                    throw new MetadataInterruption(TokenMetadataProviderHelper.GetLocalVariableMetadata(varName, _proxy, _parseContext));
                }
            }
        }
        internal void HandleAutocompletion(Expression expression)
        {
            if (!_forAutoCompletion) return;

            if (lastScanResult.Line == _stopAtRow &&
                lastScanResult.Start <= _stopAtColumn &&
                lastScanResult.End >= _stopAtColumn)
            {
                throw new AutoCompletionTypeInterruption(_typeInferer.InferType(expression, lastScanResult.Token.TokenType == TType.StaticDoubleDot, _parseContext));
            }
        }

        internal SourceCodeBinding DebuggeableBinding(IScanResult scanResult)
        {
            return SourceCodeBinding(scanResult, scanResult);
        }

        internal SourceCodeBinding SourceCodeBinding(IScanResult fromScan, IScanResult toScan)
        {
            return new SourceCodeBinding(fromScan.Line, fromScan.Start, toScan.Line, toScan.End);
        }

        internal SourceCodeBinding SourceCodeBinding(SourceCodeBinding fromSourceCodeBinding, IScanResult toScan)
        {
            return new SourceCodeBinding(fromSourceCodeBinding.FromLine, fromSourceCodeBinding.FromPosition, toScan.Line, toScan.End);
        }

        internal SourceCodeBinding SourceCodeBinding(IScanResult fromScan, SourceCodeBinding toSourceCodeBinding)
        {
            return new SourceCodeBinding(fromScan.Line, fromScan.Start, toSourceCodeBinding.ToLine, toSourceCodeBinding.ToPosition);
        }

        internal SourceCodeBinding SourceCodeBinding(IScanResult scanResult)
        {
            return new SourceCodeBinding(scanResult.Line, scanResult.Start, scanResult.Line, scanResult.End);
        }
    }
}
