using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Core;
using XppInterpreter.Lexer;

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
        private readonly Interpreter.Proxy.IIntrinsicFunctionProvider _intrinsicFunctionProvider;
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

        public XppParser(ILexer lexer, Interpreter.Proxy.IIntrinsicFunctionProvider intrinsicFunctionProvider)
        {
            this._lexer = lexer;
            this._intrinsicFunctionProvider = intrinsicFunctionProvider;
        }

        public IAstNode Parse()
        {
            Initialize();

            return Program();
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
            var start = Match(TType.LeftBrace);
            var stmts = new List<Statement>();

            while (currentToken.TokenType != TType.RightBrace && currentToken.TokenType != TType.EOF)
            {
                stmts.Add(Statement());
            }

            var end = Match(TType.RightBrace);

            return new Block(stmts, SourceCodeBinding(start, end), DebuggeableBinding(start));
        }

        internal Constructor Constructor()
        {
            var start = currentScanResult;

            Match(TType.New);
            Word identifier = (Word)Match(TType.Id).Token;

            return new Constructor(
                identifier, 
                Parameters(), 
                null, 
                false, 
                SourceCodeBinding(start, lastScanResult),
                SourceCodeBinding(start, lastScanResult));
        }

        internal List<string> IntrinsicParameters(string functionName)
        {
            List<string> literalParameters = new List<string>();
            Match(TType.LeftParenthesis);

            while (currentToken.TokenType != TType.RightParenthesis)
            {
                var parameter = MatchMultiple(TType.Id, TType.String).Token;

                string literalValue = string.Empty;

                if (parameter is Word word) literalValue = word.Lexeme;
                else if (parameter is Lexer.String str) literalValue = (string)str.Value;

                literalParameters.Add(literalValue);

                if (currentToken.TokenType != TType.RightParenthesis)
                {
                    Match(TType.Comma);
                }
            }

            Match(TType.RightParenthesis);

            return literalParameters;
        }

        internal List<Expression> Parameters()
        {
            List<Expression> parameters = new List<Expression>();

            Match(TType.LeftParenthesis);

            while (currentToken.TokenType != TType.RightParenthesis)
            {
                parameters.Add(Expression());

                if (currentToken.TokenType != TType.RightParenthesis)
                {
                    Match(TType.Comma);
                }
            }

            Match(TType.RightParenthesis);

            return parameters;
        }

        internal Expression IntrinsicFunction(string functionName)
        {
            var start = lastScanResult;
            var parameters = IntrinsicParameters(functionName);
            object result = null;
            Expression ret = null;

            try
            {
                // Call intrinsic function
                result = Interpreter.Proxy.XppProxyHelper.CallIntrinsicFunction(_intrinsicFunctionProvider, functionName, parameters.ToArray<object>());
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                if (ex.InnerException != null) message = ex.InnerException.Message;
                ThrowParseException(message, false);
            }

            var binding = SourceCodeBinding(start, lastScanResult);

            if (result is int intValue) ret = new Constant(intValue, binding);
            else if (result is string strValue) ret = new Constant(strValue, binding);
            else if (result is object[] conValue) ret = new Constant(conValue, binding);
            else if (result != null) ret = new Constant(result, binding);
            else ThrowParseException($"Unexpected compile-time function {functionName} result.");

            return ret;
        }

        internal Expression Variable(Expression caller = null, bool staticCall = false)
        {
            var identifier = Match(TType.Id);
            Expression ret;
            SourceCodeBinding debuggeableStartBinding = caller?.SourceCodeBinding ?? new SourceCodeBinding(identifier.Line, identifier.Start, 0, 0);

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

                    var parameters = Parameters();

                    ret = new FunctionCall(
                        (Word)identifier.Token,
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

            if (currentToken.TokenType == TType.Dot || currentToken.TokenType == TType.StaticDoubleDot)
            {
                if (currentToken.TokenType == TType.StaticDoubleDot &&
                    (ret is FunctionCall || ret is ArrayAccess))
                {
                    ThrowParseException($"Syntax error. Token {currentToken.TokenType} was not expected.");
                }

                var match = MatchMultiple(TType.Dot, TType.StaticDoubleDot);
                ret = Variable(ret, match.Token.TokenType == TType.StaticDoubleDot);
            }


            return ret;
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
                        ThrowParseException("Default part must be the last case in switch statement.");
                    }
                    else if (currentToken.TokenType == TType.Default)
                    {
                        ThrowParseException("Switch statements may not have multie default parts.");
                    }
                }

                if (currentToken.TokenType == TType.Case)
                {
                    if (cases is null) cases = new Dictionary<Expression, List<Statement>>();

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

            return new While(condition, Block(), 
                SourceCodeBinding(bindingStart, lastScanResult),
                SourceCodeBinding(bindingStart, bindingEnds));
        }

        internal For For()
        {
            var start = Match(TType.For);
            Match(TType.LeftParenthesis);
            Statement initialisation = Statement(false);
            Match(TType.Semicolon);
            Expression expression = Expression();
            Match(TType.Semicolon);
            Statement loopStmt = Statement(false);
            var end = Match(TType.RightParenthesis);

            return new For(initialisation, expression, loopStmt, Block(), SourceCodeBinding(start, lastScanResult), SourceCodeBinding(start, end));
        }

        internal Do Do()
        {
            var start = Match(TType.Do);
            Block block = Block();
            Match(TType.While);
            Match(TType.LeftParenthesis);
            Expression expression = Expression();
            Match(TType.RightParenthesis);
            Match(TType.Semicolon);
            return new Do(expression, block, SourceCodeBinding(start, lastScanResult), DebuggeableBinding(start));
        }

        internal VariableDeclarations VariableDeclaration(bool matchSemicolon = true)
        {
            Dictionary<Word, Expression> declarations = new Dictionary<Word, Expression>();
            Word arrayIdentifier = null;
            Expression arraySize = null;

            Token type = currentToken;
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
                TType.TypeDatetime,
                TType.Var);

            bool isArray = false;
            do
            {
                if (declarations.Count > 0 && !isArray)
                {
                    Match(TType.Comma);
                }

                Word id = (Word)Match(TType.Id).Token;
                Expression initialisation = null;

                if (currentToken.TokenType == TType.Assign)
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
            } while (currentToken.TokenType == TType.Comma);


            if (matchSemicolon)
                Match(TType.Semicolon);

            if (isArray)
                return new VariableArrayDeclaration((Word)type, arrayIdentifier, arraySize, SourceCodeBinding(start, lastScanResult));
            else
                return new VariableDeclarations((Word)type, declarations, SourceCodeBinding(start, lastScanResult));
        }

        internal LoopControl LoopControl()
        {
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

        internal Statement Statement(bool matchSemicolon = true)
        {
            switch (currentToken.TokenType)
            {
                case TType.Void: 
                case TType.LeftBrace: return Block();
                case TType.Id:
                    {
                        // Check if the next token is an Id
                        var nextToken = AdvancePeek(false).Token;
                        if (nextToken.TokenType == TType.Id)
                        {
                            nextToken = AdvancePeek(true).Token;
                            if (nextToken.TokenType == TType.LeftParenthesis)
                                return FunctionDeclaration();
                            else
                                return VariableDeclaration(matchSemicolon);
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
                case TType.InsertRecordset: return InsertRecordset();
                case TType.UpdateRecordset: return UpdateRecordset();
                case TType.DeleteFrom: return DeleteFrom();
                case TType.Select: return SelectStatement();
                case TType.While:
                    {
                        var nextToken = AdvancePeek(true).Token;
                        if (nextToken.TokenType == TType.Select)
                            return WhileSelect();
                        else
                            return While();
                    }
                case TType.For: return For();
                case TType.Do: return Do();
                case TType.Break:
                case TType.Continue:
                    return LoopControl();
                case TType.Switch:
                    return Switch();
                case TType.ChangeCompany: return ChangeCompany();
                case TType.Var:
                case TType.TypeStr:
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
                            return FunctionDeclaration();
                        else
                            return VariableDeclaration(matchSemicolon);
                    }
                default:
                    {
                        ThrowParseException("Invalid syntax.");
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
                TType.TypeDatetime,
                TType.Var,
                TType.Void);

            var funcNameToken = Match(TType.Id).Token;
            Match(TType.LeftParenthesis);

            List<FunctionDeclarationParameter> parameters = new List<FunctionDeclarationParameter>();

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

            return new FunctionDeclaration(
                ((Word)funcNameToken).Lexeme, 
                start.Token, 
                parameters,
                block,
                SourceCodeBinding(start, lastScanResult));
        }

        FunctionDeclarationParameter FunctionDeclarationParameter()
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
                TType.TypeDatetime);

            var id = Match(TType.Id).Token;

            return new FunctionDeclarationParameter(start.Token, ((Word)id).Lexeme, SourceCodeBinding(start, lastScanResult));
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
                elements.Add(Expression());

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
                    return new Constant((string)(stringScan.Token as BaseType).Value, SourceCodeBinding(stringScan));

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
                    ThrowParseException($"Syntax error. Token {token} was not expected.");
                    return null;
            }
        }

        internal Expression Factor()
        {
            Expression expr = Primary();

            while (currentToken.TokenType == TType.Star 
                || currentToken.TokenType == TType.Division 
                || currentToken.TokenType == TType.Mod 
                || currentToken.TokenType == TType.IntegerDivision)
            {
                var result = Match(currentToken.TokenType);
                expr = new BinaryOperation(
                    expr,
                    Primary(),
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

            while (currentToken.TokenType == TType.Plus || currentToken.TokenType == TType.Minus)
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
                || currentToken.TokenType == TType.Like)
            {
                if ((currentToken.TokenType == TType.Like 
                   || currentToken.TokenType == TType.In) 
                    && !isParsingWhereStatement)
                {
                    ThrowParseException("In and Like statements can only be used in queries.");
                }

                var result = Match(currentToken.TokenType);
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
                    ThrowParseException("In statement can only be compared to a container variable.");
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

        internal Statement Assignment(bool matchSemicolon = true)
        {
            var start = currentScanResult;
            Variable assignee = (Variable)Variable();
            Token operand = currentToken;
            Statement ret = null;
            switch (currentToken.TokenType)
            {
                case TType.Assign:
                    {
                        Match(TType.Assign);
                        ret = new Assignment(assignee, Expression(), SourceCodeBinding(start, lastScanResult));
                    }
                    break;

                case TType.Increment:
                case TType.Decrement:
                    {   
                        Match(currentToken.TokenType);
                        ret = new Assignment(assignee, new BinaryOperation(assignee, new Constant(1, null), operand, null), SourceCodeBinding(start, lastScanResult));
                    }
                    break;

                case TType.PlusAssignment:
                case TType.MinusAssignment:
                    {
                        Match(currentToken.TokenType);
                        var binding = SourceCodeBinding(start, currentScanResult);
                        ret = new Assignment(
                            assignee, 
                            new BinaryOperation(assignee, Expression(), operand, SourceCodeBinding(start, currentScanResult)),
                            binding);
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
                            ThrowParseException("Syntax error.");
                        }
                    }
                    break;
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

        void ThrowParseException(string s, bool showLine = false)
        {   
            throw new ParseException(
                s,
                currentToken,
                currentScanResult.Line,
                currentScanResult.End,
                showLine);
        }

        IScanResult MatchMultiple(params TType[] ttypes)
        {
            if (ttypes.Contains(currentScanResult.Token.TokenType))
            {
                Move();
            }
            else
            {
                ThrowParseException($"Syntax error: {string.Join(", ", ttypes)} expected.");
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
                ThrowParseException($"Syntax error: {ttype} was expected.");
            }

            // Move function sets the last scan results
            return lastScanResult;
        }

        void Initialize()
        {
            if (!hasBeenInitialized)
            {
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
