using System;
using System.Collections.Generic;
using System.Linq;
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
        private bool validateReturnType = true;

        // @SYS12345 or @ApplicationSuite:SomeLabel
        private static readonly System.Text.RegularExpressions.Regex LabelId = new System.Text.RegularExpressions.Regex(
            @"^@[A-Za-z_][A-Za-z0-9_]*(:[A-Za-z0-9_]+)?$", System.Text.RegularExpressions.RegexOptions.Compiled);

        // The .NET type of the new statement whose arguments are being parsed
        private System.Type _constructorType;

        // Null at the top level of the script, where a return takes a value or not
        private bool? _isVoidFunction;

        private ParseContext _parseContext = new ParseContext();
        private readonly ILexer _lexer;
        private readonly Interpreter.Proxy.XppProxy _proxy;
        private readonly ParseErrorCollection _parseErrors = new ParseErrorCollection();

        private bool _forAutoCompletion;
        private bool _forMetadata;
        private AutoCompletionPurpose _autoCompletionPurpose;
        private int _stopAtRow, _stopAtColumn;
        private XppTypeInferer _typeInferer = null;

        // The closing >> of nested generics is one token, and it was seen by the inner type
        private bool _closingGenericsPending;

        private static readonly TType[] BuiltInTypes =
        {
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
            TType.TypeDatetime
        };

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

            if (lexer is XppLexer xppLexer && xppProxy != null)
            {
                xppLexer.ExpandMacros(xppProxy.Reflection.GetMacroLibrary);
            }
        }

        public static ParseResult ParseExpressionWithContext(ILexer lexer, Interpreter.Proxy.XppProxy xppProxy, ParseContext parseContext)
        {
            var parser = new XppParser(lexer, xppProxy);
            parser._parseContext = parseContext;
            parser.validateReturnType = false;
            parser.Initialize();
            Expression expression = null;
            try
            {
                expression = parser.Expression();
            }
            catch (ParseException ex)
            {
                parser._parseErrors.Add(new ParseError(ex.Token, -1, -1, ex.Message));
            }

            return new ParseResult(expression, parser._parseErrors);
        }

        public ParseResult Parse()
        {
            Program program = null;

            try
            {
                Initialize();
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

        /// <returns>What the position is about, or null when nothing can be said</returns>
        public AutoCompletionTypeInterruption ParseForAutoCompletion(int row, int column, AutoCompletionPurpose purpose)
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
                return interruption;
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

            ParsedType type = TypeName(false);
            Word identifier = type.Word;

            // new System.String[n]()
            if (type.ClrType != null && currentToken.TokenType == TType.LeftBracket)
            {
                return ClrArrayConstructor(start, type);
            }

            var enclosingConstructorType = _constructorType;

            _constructorType = type.ClrType;

            var parameters = Parameters(null, identifier.Lexeme, false, true);

            _constructorType = enclosingConstructorType;

            if (type.ClrType != null)
            {
                var argumentTypes = InferredArgumentTypes(parameters);

                if (argumentTypes != null && !Core.Clr.ClrBinder.HasConstructor(type.ClrType, argumentTypes))
                {
                    HandleParseError(MethodNotFoundMessage(type.ClrType, "Constructor", argumentTypes), stop: false);
                }
            }

            return new Constructor(
                identifier,
                parameters,
                null,
                false,
                SourceCodeBinding(start, lastScanResult),
                SourceCodeBinding(start, lastScanResult))
            {
                ClrType = type.ClrType,
                ArgumentTypes = ClrArgumentTypes(parameters)
            };
        }

        private Constructor ClrArrayConstructor(IScanResult start, ParsedType type)
        {
            var sizes = new List<Expression>();

            Match(TType.LeftBracket);

            do
            {
                if (currentToken.TokenType == TType.Comma)
                {
                    Match(TType.Comma);
                }

                sizes.Add(Expression());
            } while (currentToken.TokenType == TType.Comma);

            Match(TType.RightBracket);

            if (currentToken.TokenType == TType.LeftParenthesis)
            {
                Match(TType.LeftParenthesis);
                Match(TType.RightParenthesis);
            }

            return new Constructor(
                type.Word,
                sizes,
                null,
                false,
                SourceCodeBinding(start, lastScanResult),
                SourceCodeBinding(start, lastScanResult))
            {
                ClrType = sizes.Count == 1 ? type.ClrType.MakeArrayType() : type.ClrType.MakeArrayType(sizes.Count)
            };
        }

        /// <param name="allowArray">The name can be followed by [] or [,]</param>
        /// <param name="otherTokens">Tokens that are also accepted as the type</param>
        private ParsedType TypeName(bool allowArray, params TType[] otherTokens)
        {
            var start = currentScanResult;

            if (currentToken.TokenType != TType.Id)
            {
                var result = MatchMultiple(otherTokens);

                return new ParsedType(result.Token as Word, result, null);
            }

            Word first = (Word)Match(TType.Id).Token;
            string name = first.Lexeme;

            while (currentToken.TokenType == TType.Dot)
            {
                Match(TType.Dot);
                HandleNamespaceAutocompletion(name);
                name += "." + WordText(MatchAnyWord());
            }

            var typeArguments = currentToken.TokenType == TType.Smaller ? GenericArguments() : null;

            System.Type clrType = ResolveClrType(name, typeArguments);

            if (clrType is null && (name.Contains('.') || typeArguments != null))
            {
                HandleParseError(string.Format(MessageProvider.ExceptionTypeNotFound, name), stop: false);
            }

            if (allowArray && clrType != null && currentToken.TokenType == TType.LeftBracket)
            {
                clrType = ClrArrayType(clrType);
            }

            return new ParsedType(clrType != null ? new Word(name, TType.Id) : first, start, clrType);
        }

        private System.Type ClrArrayType(System.Type elementType)
        {
            Match(TType.LeftBracket);

            int rank = 1;

            while (currentToken.TokenType == TType.Comma)
            {
                Match(TType.Comma);
                rank++;
            }

            Match(TType.RightBracket);

            return rank == 1 ? elementType.MakeArrayType() : elementType.MakeArrayType(rank);
        }

        private List<System.Type> GenericArguments()
        {
            var arguments = new List<System.Type>();

            Match(TType.Smaller);

            do
            {
                if (currentToken.TokenType == TType.Comma)
                {
                    Match(TType.Comma);
                }

                ParsedType argument = TypeName(true, BuiltInTypes);

                System.Type type = argument.ClrType ?? _proxy.Casting.GetSystemTypeFromTypeName(argument.Word.Lexeme);

                if (type is null)
                {
                    HandleParseError(string.Format(MessageProvider.ExceptionTypeNotFound, argument.Word.Lexeme), stop: false);
                    type = typeof(object);
                }

                arguments.Add(type);
            } while (currentToken.TokenType == TType.Comma);

            if (_closingGenericsPending)
            {
                _closingGenericsPending = false;
                Match(TType.RightShift);
            }
            else if (currentToken.TokenType == TType.RightShift)
            {
                _closingGenericsPending = true;
            }
            else
            {
                Match(TType.Greater);
            }

            return arguments;
        }

        /// <summary>
        /// Finds the .NET type of a name, applying the using directives. X++ types come first.
        /// </summary>
        private System.Type ResolveClrType(string name, List<System.Type> typeArguments)
        {
            int arity = typeArguments?.Count ?? 0;

            if (arity == 0 && !name.Contains('.') && _typeInferer.IsKnownType(name) && !_parseContext.UsingAliases.ContainsKey(name))
            {
                return null;
            }

            System.Type type = FindClrType(name, arity);

            if (type != null && arity > 0)
            {
                try
                {
                    type = type.MakeGenericType(typeArguments.ToArray());
                }
                catch (ArgumentException exception)
                {
                    HandleParseError(exception.Message);
                }
            }

            return type;
        }

        private System.Type FindClrType(string name, int arity)
        {
            string resolved = ApplyUsingAlias(name);

            System.Type type = _proxy.ClrTypes.Find(resolved, arity);

            if (type != null)
            {
                return type;
            }

            foreach (var @namespace in _parseContext.Usings)
            {
                type = _proxy.ClrTypes.Find($"{@namespace}.{name}", arity);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private string ApplyUsingAlias(string name)
        {
            int dot = name.IndexOf('.');
            string first = dot < 0 ? name : name.Substring(0, dot);

            if (_parseContext.UsingAliases.TryGetValue(first, out var target))
            {
                return dot < 0 ? target : target + name.Substring(dot);
            }

            return name;
        }

        private static string WordText(IScanResult scanResult)
        {
            return scanResult.Text ?? ((Word)scanResult.Token).Lexeme;
        }

        private Token PeekToken(int offset)
        {
            return offset < 0 ? currentToken : _lexer.Peek(offset).Token;
        }

        /// <returns>The offset for <see cref="ILexer.Peek"/> of its last token, -1 when it's only the current token</returns>
        private int SkipTypeName()
        {
            int last = -1;

            while (PeekToken(last + 1).TokenType == TType.Dot && PeekToken(last + 2) is Word)
            {
                last += 2;
            }

            if (PeekToken(last + 1).TokenType == TType.Smaller)
            {
                int end = SkipGenericArguments(last + 1);

                if (end < 0)
                {
                    return last;
                }

                last = end;
            }

            if (PeekToken(last + 1).TokenType == TType.LeftBracket)
            {
                int bracket = last + 2;

                while (PeekToken(bracket).TokenType == TType.Comma)
                {
                    bracket++;
                }

                if (PeekToken(bracket).TokenType == TType.RightBracket)
                {
                    last = bracket;
                }
            }

            return last;
        }

        /// <returns>The offset of the closing token, -1 when these tokens can't be type arguments</returns>
        private int SkipGenericArguments(int offset)
        {
            int depth = 0;

            while (true)
            {
                Token token = PeekToken(offset);

                switch (token.TokenType)
                {
                    case TType.Smaller:
                        depth++;
                        break;
                    case TType.Greater:
                        depth--;
                        break;
                    case TType.RightShift:
                        depth -= 2;
                        break;
                    case TType.Dot:
                    case TType.Comma:
                    case TType.LeftBracket:
                    case TType.RightBracket:
                    case TType.Id:
                        break;
                    default:
                        if (!BuiltInTypes.Contains(token.TokenType))
                        {
                            return -1;
                        }
                        break;
                }

                if (depth <= 0)
                {
                    return offset;
                }

                offset++;
            }
        }

        /// <summary>
        /// Tells if the statement at the current token declares a variable or a function: <c>A.B&lt;str&gt;[] name</c>
        /// </summary>
        private bool IsDeclarationAhead(out bool isFunction)
        {
            int last = SkipTypeName();

            isFunction = PeekToken(last + 2).TokenType == TType.LeftParenthesis;

            return PeekToken(last + 1).TokenType == TType.Id;
        }

        /// <returns>The reference to the type, or null when the current token is something else</returns>
        private Variable ClrTypeReference()
        {
            string name = ((Word)currentToken).Lexeme;

            if (_parseContext.CurrentScope.FindVariableDeclaration(name) != null)
            {
                return null;
            }

            int last = SkipTypeName();

            if (PeekToken(last + 1).TokenType != TType.StaticDoubleDot)
            {
                return null;
            }

            // A simple name is an X++ type unless the using directives say something else
            if (last == -1 && ResolveClrType(name, null) is null)
            {
                return null;
            }

            ParsedType type = TypeName(false);

            if (type.ClrType is null)
            {
                HandleParseError(string.Format(MessageProvider.ExceptionTypeNotFound, type.Word.Lexeme));
            }

            return new Variable(type.Word, null, false, SourceCodeBinding(type.Start, lastScanResult))
            {
                ClrType = type.ClrType
            };
        }

        /// <summary>
        /// The declared .NET types of the arguments that are variables. They decide the overload when the value is null.
        /// </summary>
        private System.Type[] ClrArgumentTypes(List<Expression> arguments)
        {
            System.Type[] types = null;

            for (int i = 0; i < arguments.Count; i++)
            {
                if (arguments[i] is Variable variable && !(variable is FunctionCall) && variable.Caller is null)
                {
                    var declaration = _parseContext.CurrentScope.FindVariableDeclaration(variable.Name);

                    if (declaration != null && declaration.IsClrType)
                    {
                        if (types is null)
                        {
                            types = new System.Type[arguments.Count];
                        }

                        types[i] = declaration.ClrType;
                    }
                }
            }

            return types;
        }

        private Statement UsingDirective()
        {
            var start = Match(TType.Using);

            string alias = null;

            if (PeekToken(0).TokenType == TType.Assign)
            {
                alias = ((Word)Match(TType.Id).Token).Lexeme;
                Match(TType.Assign);
            }

            string name = ((Word)Match(TType.Id).Token).Lexeme;

            while (currentToken.TokenType == TType.Dot)
            {
                Match(TType.Dot);
                HandleNamespaceAutocompletion(name);
                name += "." + WordText(MatchAnyWord());
            }

            Match(TType.Semicolon);

            if (alias != null)
            {
                _parseContext.UsingAliases[alias] = name;
            }
            else
            {
                _parseContext.Usings.Add(name);
            }

            return new Block(new List<Statement>(), SourceCodeBinding(start, lastScanResult), null);
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
                parameters.Add(currentToken.TokenType == TType.ByRef ? ByRefArgument() : Expression());

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

        private Expression ByRefArgument()
        {
            Match(TType.ByRef);

            Expression argument = Variable(expectReturn: false);

            if (argument is Variable variable && !(argument is FunctionCall) && !(argument is ArrayAccess) && variable.Caller is null)
            {
                ValidateNotConst(argument);
                variable.IsByRef = true;
            }
            else
            {
                HandleParseError(MessageProvider.ExceptionInvalidByRef, stop: false);
            }

            return argument;
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
            else if (result?.GetType() == _proxy.Reflection.GetDateType())
            {
                ret = new Constant(new BaseType(result, TType.Date), binding);
            }

            return ret;
        }

        internal Expression Variable(Expression caller = null, bool staticCall = false, bool expectReturn = true)
        {
            if (caller is null && currentToken.TokenType == TType.Id)
            {
                HandleDottedNameAutocompletion();

                Variable clrType = ClrTypeReference();

                if (clrType != null)
                {
                    Match(TType.StaticDoubleDot);
                    HandleAutocompletion(clrType);

                    return Variable(clrType, true, expectReturn);
                }
            }

            // record.(fieldId)
            if (caller != null && !staticCall && currentToken.TokenType == TType.LeftParenthesis)
            {
                var fieldIdStart = Match(TType.LeftParenthesis);
                Expression fieldId = Expression();
                var fieldIdEnd = Match(TType.RightParenthesis);

                return new FieldByIdAccess(caller, fieldId, SourceCodeBinding(fieldIdStart, fieldIdEnd));
            }

            IScanResult identifier = caller is null ? Match(TType.Id) : MatchAnyWord();

            // A member named like an X++ keyword (Count, Exists, Throw) keeps the casing it was written with, for the messages
            if (caller != null && identifier.Text != null && identifier.Token.TokenType != TType.Id)
            {
                identifier = new ScanResult(new Word(identifier.Text, TType.Id), identifier.Start, identifier.End, identifier.Line);
            }

            // record.Map::method()
            if (caller != null && !staticCall && currentToken.TokenType == TType.StaticDoubleDot)
            {
                string mapName = ((Word)identifier.Token).Lexeme;

                if (!_typeInferer.IsKnownType(mapName))
                {
                    HandleParseError(string.Format(MessageProvider.ExceptionTypeNotFound, mapName), stop: false);
                }

                Expression map = new As(caller, mapName, SourceCodeBinding(identifier)) { IsMapCall = true };

                Match(TType.StaticDoubleDot);
                HandleAutocompletion(map);

                return Variable(map, false, expectReturn);
            }

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
                        isInsideFunctionScope ? SourceCodeBinding(debuggeableStartBinding, lastScanResult) : null)
                    {
                        ArgumentTypes = ClrArgumentTypes(parameters)
                    };
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
                    // Only X++ arrays take [index]: a .NET list or array needs get_Item, GetValue and SetValue, as compiled X++ requires
                    var indexedVariable = caller is null ? _parseContext.CurrentScope.FindVariableDeclaration(((Word)identifier.Token).Lexeme) : null;

                    if (indexedVariable != null && !indexedVariable.IsArray)
                    {
                        HandleParseError(MessageProvider.ExceptionArrayIndexingClrType, stop: false);
                    }

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
                    if (ret is Variable systemVariable && SystemVariables.IsSystemVariable(variableName))
                    {
                        systemVariable.IsSystemVariable = true;
                    }
                    else
                    {
                        HandleParseError(string.Format(MessageProvider.ExceptionVariableNotDeclared, variableName), stop: false);
                    }
                }
            }

            System.Type returnType = _typeInferer.InferType(ret, currentToken.TokenType == TType.StaticDoubleDot, _parseContext);

            if (validateReturnType && ret is FunctionCall && expectReturn && ((returnType is null && !CanHaveUnknownReturnType((FunctionCall)ret)) || returnType == typeof(void)))
            {
                HandleParseError(MessageProvider.ExceptionTypeCannotBeNull, stop: false);
            }

            if (ret is Variable variable && variable.Caller != null)
            {
                var callerType = _typeInferer.InferType(variable.Caller, variable.StaticCall, _parseContext);
                ValidateVariableMember(callerType, ret, variable.StaticCall);
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

        /// <summary>
        /// A .NET method whose overloads return different types has no known type here, and neither has a call on an X++ Object.
        /// </summary>
        private bool CanHaveUnknownReturnType(FunctionCall functionCall)
        {
            if (functionCall.Caller is null) return false;

            System.Type callerType = _typeInferer.InferType(functionCall.Caller, functionCall.StaticCall, _parseContext);

            return _typeInferer.IsClrType(callerType) || _typeInferer.IsLateBoundType(callerType);
        }

        private void ValidateVariableMember(System.Type callerType, Expression expression, bool isStatic)
        {
            if (callerType is null || _typeInferer.IsLateBoundType(callerType)) return;

            if (_typeInferer.IsClrType(callerType))
            {
                if (expression is FunctionCall clrCall)
                {
                    // The compiler names the argument types when the method is missing, generic or has no such overload
                    var argumentTypes = InferredArgumentTypes(clrCall.Parameters);

                    if (argumentTypes != null)
                    {
                        if (!Core.Clr.ClrBinder.HasOverload(callerType, clrCall.Name, isStatic, argumentTypes))
                        {
                            HandleParseError(MethodNotFoundMessage(callerType, clrCall.Name, argumentTypes), stop: false);
                        }
                    }
                    else if (!Core.Clr.ClrBinder.HasMethod(callerType, clrCall.Name, isStatic))
                    {
                        HandleParseError(string.Format(MessageProvider.ExceptionInvalidTypeMethod, callerType.Name, clrCall.Name), stop: false);
                    }
                }
                else if (expression is Variable clrMember && !Core.Clr.ClrBinder.HasMember(callerType, clrMember.Name, isStatic))
                {
                    HandleParseError(string.Format(MessageProvider.ExceptionInvalidTypeField, callerType.Name, clrMember.Name), stop: false);
                }

                return;
            }


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

        /// <summary>
        /// The types of the arguments of a .NET call when the parser knows every one of them, for the compiler's overload check.
        /// Null when one is unknown: a late-bound object, an anytype or a container, which the X++ compiler accepts for many parameters.
        /// </summary>
        private System.Type[] InferredArgumentTypes(List<Expression> arguments)
        {
            var types = new System.Type[arguments.Count];

            for (int i = 0; i < arguments.Count; i++)
            {
                types[i] = _typeInferer.InferType(arguments[i], _parseContext);

                if (types[i] is null || types[i] == typeof(object) || types[i] == typeof(object[]) || _typeInferer.IsLateBoundType(types[i]))
                {
                    return null;
                }
            }

            return types;
        }

        /// <summary>
        /// The compiler's message: Method 'Name(System.String)' is not found on type 'T'. A generic type is named without its arguments.
        /// </summary>
        private static string MethodNotFoundMessage(System.Type type, string name, System.Type[] argumentTypes)
        {
            string arguments = string.Join(", ", argumentTypes.Select(t => t.IsGenericType ? t.Namespace + "." + t.Name : t.FullName));

            return string.Format(MessageProvider.ExceptionMethodNotFoundForArguments, name, arguments, type.FullName);
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

        internal KeyValuePair<Expression, List<Statement>> Case(IDictionary<Expression, List<Expression>> extraCaseValues)
        {
            var caseResult = Match(TType.Case);
            var expression = Expression();
            var statements = new List<Statement>();

            // case 1, 2, 3:
            while (currentToken.TokenType == TType.Comma)
            {
                Match(TType.Comma);

                if (!extraCaseValues.TryGetValue(expression, out var values))
                {
                    values = new List<Expression>();
                    extraCaseValues[expression] = values;
                }

                values.Add(Expression());
            }

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
            var extraCaseValues = new Dictionary<Expression, List<Expression>>();

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

                    cases.Add(Case(extraCaseValues));
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
                SourceCodeBinding(switchResult, switchResultEnd),
                extraCaseValues);
        }

        internal If If(IScanResult elseScan = null)
        {
            var ifResult = Match(TType.If);
            var debuggeableStartResult = elseScan is null ? ifResult : elseScan;

            Match(TType.LeftParenthesis);
            Expression expression = Expression();

            var endIfResult = Match(TType.RightParenthesis);

            Block block = BlockOrStatement();

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
                return new While(condition, BlockOrStatement(),
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
                @for = new For(initialisation, expression, loopStmt, BlockOrStatement(),
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
                Block block = BlockOrStatement();
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

        internal VariableDeclarations VariableDeclaration(bool matchSemicolon = true, bool isUsing = false, bool isConst = false)
        {
            Dictionary<Word, Expression> declarations = new Dictionary<Word, Expression>();
            var arrays = new List<VariableArrayDeclaration>();

            ParsedType parsedType = TypeName(true, BuiltInTypes.Concat(new[] { TType.Var }).ToArray());

            var typeResult = parsedType.Start;
            Word typeWord = parsedType.Word;
            bool isClrType = parsedType.ClrType != null;

            if (!isClrType && typeWord.TokenType != TType.Var && !_typeInferer.IsKnownType(typeWord.Lexeme))
            {
                HandleParseError(string.Format(MessageProvider.ExceptionTypeNotFound, typeWord.Lexeme), stop: false);
            }

            // str 20 name;
            int stringLength = 0;

            if (typeWord.TokenType == TType.TypeStr && currentToken.TokenType == TType.Int32)
            {
                stringLength = (int)((BaseType)Match(TType.Int32).Token).Value;
            }

            // Get type from type declaration if it's a known type
            System.Type declarationType = parsedType.ClrType
                ?? (typeWord.TokenType != TType.Var && _typeInferer.IsKnownType(typeWord.Lexeme) ? _proxy.Casting.GetSystemTypeFromTypeName(typeWord.Lexeme) : null);

            bool isFirst = true;
            do
            {
                if (isUsing && !isFirst)
                {
                    HandleParseError(MessageProvider.ExceptionInvalidUsing, stop: false);
                }

                if (!isFirst)
                {
                    Match(TType.Comma);
                }

                isFirst = false;

                Word id = (Word)Match(TType.Id).Token;
                Expression initialisation = null;

                if (SystemVariables.IsSystemVariable(id.Lexeme))
                {
                    HandleParseError(string.Format(MessageProvider.ExceptionSystemVariableDeclared, id.Lexeme), stop: false);
                }

                if (isConst && currentToken.TokenType != TType.Assign)
                {
                    HandleParseError(MessageProvider.ExceptionConstWithoutValue);
                }

                if (isUsing || currentToken.TokenType == TType.Assign)
                {
                    Match(TType.Assign);
                    initialisation = Expression();

                    if (isConst && !IsConstantExpression(initialisation))
                    {
                        HandleParseError(string.Format(MessageProvider.ExceptionConstNotConstant, id.Lexeme), stop: false);
                    }
                }
                else if (currentToken.TokenType == TType.LeftBracket)
                {
                    if (isClrType)
                    {
                        HandleParseError(MessageProvider.ExceptionClrTypeInXppArray);
                    }

                    var arrayStart = Match(TType.LeftBracket);
                    Expression arraySize = currentToken.TokenType != TType.RightBracket ? Expression() : null;

                    Match(TType.RightBracket);

                    var array = new VariableArrayDeclaration(typeWord, _proxy.Casting.GetArrayType(declarationType), id, arraySize, SourceCodeBinding(arrayStart, lastScanResult));

                    arrays.Add(array);
                    _parseContext.CurrentScope.VariableDeclarations.Add(
                        new ParseContextScopeVariable(id.Lexeme, typeWord, array.DeclarationClrType, true, null));
                    continue;
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
                    else if (initializationType != null && !CanAssign(initializationType, declarationType))
                    {
                        HandleParseError(string.Format(MessageProvider.ExceptionImplicitConversion, initializationType.Name, declarationType.Name), stop: false);
                    }
                }

                // The next declarations of the statement can use it
                _parseContext.CurrentScope.VariableDeclarations.Add(
                    new ParseContextScopeVariable(id.Lexeme, typeWord, declarationType, false, initialisation)
                    {
                        IsClrType = isClrType,
                        IsConst = isConst
                    });
            } while (currentToken.TokenType == TType.Comma);

            if (matchSemicolon)
            {
                Match(TType.Semicolon);
            }

            var ret = new VariableDeclarations(typeWord, declarationType, declarations, SourceCodeBinding(typeResult, lastScanResult))
            {
                StringLength = stringLength,
                IsClrType = isClrType,
                ArrayDeclarations = arrays
            };

            return ret;
        }

        /// <summary>
        /// The value of a const is known when the code is compiled: it can use literals, enum values and other consts.
        /// </summary>
        private bool IsConstantExpression(Expression expression)
        {
            switch (expression)
            {
                case Constant _:
                    return true;
                case UnaryOperation unary:
                    return IsConstantExpression(unary.Expression);
                case BinaryOperation binary:
                    return IsConstantExpression(binary.LeftOperand) && IsConstantExpression(binary.RightOperand);
                case FunctionCall _:
                    return false;
                case Variable variable:
                    return variable.StaticCall || IsConstVariable(variable);
                default:
                    return false;
            }
        }

        private bool IsConstVariable(Expression expression)
        {
            return expression is Variable variable
                && variable.Caller is null
                && _parseContext.CurrentScope.FindVariableDeclaration(variable.Name)?.IsConst == true;
        }

        private void ValidateNotConst(Expression assignee)
        {
            if (IsConstVariable(assignee))
            {
                HandleParseError(string.Format(MessageProvider.ExceptionConstCannotChange, ((Variable)assignee).Name), stop: false);
            }
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

            if (_parseContext.WithinFinally())
            {
                HandleParseError(MessageProvider.ExceptionLeaveFinally, stop: false);
            }

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

            if (inferedType != null && !typeof(IDisposable).IsAssignableFrom(inferedType))
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
            if (_parseContext.WithinFinally())
            {
                HandleParseError(MessageProvider.ExceptionLeaveFinally, stop: false);
            }

            var start = currentScanResult;

            Match(TType.Return);
            var expression = currentToken.TokenType == TType.Semicolon ? null : Expression();

            if (_isVoidFunction == true && expression != null)
            {
                HandleParseError(MessageProvider.ExceptionVoidFunctionReturnsValue, stop: false);
            }
            else if (_isVoidFunction == false && expression is null)
            {
                HandleParseError(MessageProvider.ExceptionFunctionMustReturnValue, stop: false);
            }

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
                    string exceptionVariable = null;
                    System.Type exceptionVariableType = null;

                    if (currentToken.TokenType == TType.LeftParenthesis)
                    {
                        Match(TType.LeftParenthesis);
                        var exceptionTypeExpression = Variable();

                        if (IsExceptionVariable(exceptionTypeExpression, out exceptionVariableType))
                        {
                            exceptionVariable = ((Variable)exceptionTypeExpression).Name;

                            Match(TType.RightParenthesis);

                            using (new CatchContext(_parseContext))
                            {
                                catches.Add(new Catch(null, Block())
                                {
                                    ClrType = exceptionVariableType,
                                    VariableName = exceptionVariable
                                });
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

                            continue;
                        }

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

        private bool IsExceptionVariable(Expression expression, out System.Type type)
        {
            type = null;

            if (expression is Variable variable && !(expression is FunctionCall) && !(expression is ArrayAccess) && variable.Caller is null)
            {
                var declaration = _parseContext.CurrentScope.FindVariableDeclaration(variable.Name);

                if (declaration?.ClrType != null && typeof(Exception).IsAssignableFrom(declaration.ClrType))
                {
                    type = declaration.ClrType;
                    return true;
                }
            }

            return false;
        }

        private bool CanAssign(System.Type from, System.Type to)
        {
            return Core.Clr.ClrConversions.CanConvert(from, to) || _proxy.Casting.ImplicitConversionExists(from, to);
        }

        internal Statement Statement(bool matchSemicolon = true)
        {
            switch (currentToken.TokenType)
            {
                case TType.LeftBracket: return ContainerAssignment();
                case TType.Retry: return Retry();
                case TType.Using:
                    return PeekToken(0).TokenType == TType.LeftParenthesis ? Using() : UsingDirective();
                case TType.Const:
                    Match(TType.Const);
                    return VariableDeclaration(matchSemicolon, isConst: true);
                case TType.Try: return Try();
                case TType.Print: return Print();
                case TType.Return: return Return();
                case TType.LeftBrace: return Block();
                case TType.Semicolon:
                    {
                        var emptyStatement = Match(TType.Semicolon);
                        return new Block(new List<Statement>(), SourceCodeBinding(emptyStatement), null);
                    }
                case TType.Increment:
                case TType.Decrement:
                    return PrefixIncrement(matchSemicolon);
                case TType.New:
                    return ConstructorCallStatement(matchSemicolon);
                case TType.Void:
                case TType.Id:
                    {
                        if (IsDeclarationAhead(out bool isFunction))
                        {
                            if (isFunction)
                            {
                                return FunctionDeclaration();
                            }

                            return VariableDeclaration(matchSemicolon);
                        }

                        return Assignment(matchSemicolon);
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

        private Statement PrefixIncrement(bool matchSemicolon)
        {
            var start = currentScanResult;
            Token operand = currentToken;

            Match(operand.TokenType);

            Expression assignee = Variable(expectReturn: false);

            if (!(assignee is Variable variable) || assignee is FunctionCall)
            {
                HandleParseError(MessageProvider.ExceptionInvalidSyntax);
                return null;
            }

            ValidateNotConst(assignee);

            var ret = new Assignment(variable, new BinaryOperation(assignee, new Constant(1, null), operand, null), SourceCodeBinding(start, lastScanResult));

            if (matchSemicolon)
            {
                Match(TType.Semicolon);
            }

            return ret;
        }

        /// <summary>
        /// <c>new A().b();</c> as a statement, where b may not return anything.
        /// </summary>
        private Statement ConstructorCallStatement(bool matchSemicolon)
        {
            var start = currentScanResult;

            Expression expression = MemberAccess(Constructor(), expectReturn: false);

            if (!(expression is FunctionCall functionCall) || expression is Constructor)
            {
                HandleParseError(MessageProvider.ExceptionInvalidSyntax);
                return null;
            }

            var ret = new NoReturnFunctionCall(functionCall, SourceCodeBinding(start, lastScanResult));

            if (matchSemicolon)
            {
                Match(TType.Semicolon);
            }

            return ret;
        }

        internal FunctionDeclaration FunctionDeclaration()
        {
            ParsedType parsedType = TypeName(true, BuiltInTypes.Concat(new[] { TType.Void }).ToArray());

            var start = parsedType.Start;
            Word type = parsedType.Word;

            if (parsedType.ClrType is null && type.TokenType != TType.Void && !_typeInferer.IsKnownType(type.Lexeme))
            {
                HandleParseError(string.Format(MessageProvider.ExceptionTypeNotFound, type.Lexeme), stop: false);
            }

            Word funcNameToken = Match(TType.Id).Token as Word;

            Match(TType.LeftParenthesis);

            _parseContext.FunctionDeclarationStack.New();
            _parseContext.CurrentScope.FunctionReferences.Add(new FunctionDeclarationReference((funcNameToken).Lexeme, type)
            {
                ClrType = parsedType.ClrType
            });
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

            bool? enclosingIsVoid = _isVoidFunction;

            _isVoidFunction = type.TokenType == TType.Void;

            var block = Block();

            _isVoidFunction = enclosingIsVoid;

            _parseContext.FunctionDeclarationStack.Release();
            _parseContext.CurrentScope.End();

            var ret = new FunctionDeclaration(
                ((Word)funcNameToken).Lexeme,
                type,
                parameters,
                block,
                SourceCodeBinding(start, lastScanResult));

            _parseContext.CurrentScope.FunctionDeclarations.Add(ret);

            return ret;
        }

        FunctionDeclarationParameter FunctionDeclarationParameter()
        {
            ParsedType parsedType = TypeName(true, BuiltInTypes);

            var typeResult = parsedType.Start;
            Word type = parsedType.Word;

            if (parsedType.ClrType is null && !_typeInferer.IsKnownType(type.Lexeme))
            {
                HandleParseError(string.Format(MessageProvider.ExceptionTypeNotFound, type.Lexeme), stop: false);
            }

            System.Type inferedType = parsedType.ClrType ?? _proxy.Casting.GetSystemTypeFromTypeName(type.Lexeme);

            Word id = Match(TType.Id).Token as Word;

            bool isArray = currentToken.TokenType == TType.LeftBracket;

            if (isArray)
            {
                Match(TType.LeftBracket);
                Match(TType.RightBracket);
                inferedType = _proxy.Casting.GetArrayType(inferedType);
            }

            Expression defaultValue = null;

            if (currentToken.TokenType == TType.Assign)
            {
                Match(TType.Assign);
                defaultValue = Expression();
            }

            _parseContext.CurrentScope.VariableDeclarations.Add(
                new ParseContextScopeVariable(id.Lexeme, type, inferedType, isArray)
                {
                    IsClrType = parsedType.ClrType != null
                });

            return new FunctionDeclarationParameter(type, inferedType, id.Lexeme, SourceCodeBinding(typeResult, lastScanResult), defaultValue);
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

                // X++ doesn't allow its objects in a container, but it does allow .NET ones
                if (!_typeInferer.IsClrType(elementType) && _proxy.Casting.IsReferenceType(elementType))
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
                    if (AdvancePeek(true).Token.TokenType == TType.Select)
                    {
                        return SelectExpression();
                    }
                    else
                    {
                        Match(TType.LeftParenthesis);
                        Expression node = Expression();
                        Match(TType.RightParenthesis);
                        return MemberAccess(node);
                    }

                case TType.Plus:
                case TType.Minus:
                case TType.BinaryNot:
                    var mpResult = Match(currentToken.TokenType);
                    return new UnaryOperation(mpResult.Token, Primary(), SourceCodeBinding(mpResult, lastScanResult));

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
                    string stringValue = (string)(stringScan.Token as BaseType).Value;

                    // A string that holds only a label id means the text of the label, when the label exists
                    if (LabelId.IsMatch(stringValue))
                    {
                        string labelText = _proxy.Reflection.LabelIdToValue(stringValue, null);

                        if (!string.IsNullOrEmpty(labelText))
                        {
                            stringValue = labelText;
                        }
                    }

                    return new Constant(stringValue, SourceCodeBinding(stringScan));

                case TType.Date:
                    var dateScan = Match(TType.Date);
                    var date = (DateLiteral)(dateScan.Token as Date).Value;
                    return new Constant(new BaseType(_proxy.Casting.CreateDate(date.Day, date.Month, date.Year), TType.Date), SourceCodeBinding(dateScan));

                case TType.UtcDateTime:
                    var utcScan = Match(TType.UtcDateTime);
                    var utc = (System.DateTime)(utcScan.Token as BaseType).Value;
                    // The compiler builds the literal with the constructor of the utcdatetime struct, which marks the value Utc.
                    // DateTimeUtil::newDateTime leaves the kind unspecified, also in compiled code (conformance pL25, pL33)
                    var utcValue = Core.Clr.ClrConversions.Convert(System.DateTime.SpecifyKind(utc, DateTimeKind.Utc), _proxy.Reflection.GetUtcDateTimeType());
                    return new Constant(new BaseType(utcValue, TType.UtcDateTime), SourceCodeBinding(utcScan));

                case TType.True:
                case TType.False:
                    var boolScan = Match(currentToken.TokenType);
                    return new Constant(boolScan.Token.TokenType == TType.True, SourceCodeBinding(boolScan));

                case TType.Null:
                    var nullScan = Match(TType.Null);
                    return new Constant(Word.Null, SourceCodeBinding(nullScan));

                case TType.Id:
                    if (currentToken is Word w && this.IsIdentifierMatchQueryExpressionTable(w.Lexeme))
                    {
                        return TableField();
                    }

                    return Variable();

                case TType.New:
                    return MemberAccess(Constructor());

                default:
                    HandleParseError(string.Format(MessageProvider.ExceptionTokenWasNotExpected, token));
                    return null;
            }
        }

        /// <summary>
        /// Parses the members called on a primary expression, so <c>1 + new A().b()</c> applies b to the new object and not to the addition.
        /// </summary>
        private Expression MemberAccess(Expression caller, bool expectReturn = true)
        {
            if (currentToken.TokenType != TType.Dot)
            {
                return caller;
            }

            Match(TType.Dot);
            HandleAutocompletion(caller);

            return Variable(caller, expectReturn: expectReturn);
        }

        private Block BlockOrStatement()
        {
            if (currentToken.TokenType == TType.LeftBrace)
            {
                return Block();
            }

            if (currentToken.TokenType == TType.Semicolon)
            {
                HandleParseError(MessageProvider.ExceptionEmptyStatement);
            }

            var start = currentScanResult;

            _parseContext.BeginScope();
            var statement = Statement();
            _parseContext.EndScope();

            return new Block(new List<Statement> { statement }, SourceCodeBinding(start, lastScanResult), null);
        }

        internal Expression As()
        {
            Expression expr = Primary();

            if (currentToken.TokenType == TType.As)
            {
                Match(TType.As);

                ParsedType type = TypeName(true);

                expr = new As(expr, type.Word.Lexeme, SourceCodeBinding(expr.SourceCodeBinding, lastScanResult))
                {
                    ClrType = type.ClrType
                };
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

            // && and || have the same precedence in X++: the compiler evaluates them from left to right
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
                if (currentToken.TokenType == TType.In && !isParsingWhereStatement)
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
                    ParsedType type = TypeName(true);

                    expr = new Is(
                        expr,
                        type.Word.Lexeme,
                        SourceCodeBinding(start, lastScanResult))
                    {
                        ClrType = type.ClrType
                    };
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

                ValidateNotConst(assignee);

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

        internal EventHandler EventHandler()
        {
            var start = Match(TType.EventHandler);

            Match(TType.LeftParenthesis);

            var method = Match(TType.Id).Token as Word;

            var functionDeclaration = _parseContext.CurrentScope.FindFunctionDeclarationReference(method.Lexeme);
            
            if (functionDeclaration is null)
            {
                HandleParseError($"Function {method.Lexeme} does not exist in the current context.", stop: false);
            }
            else if (functionDeclaration.ReturnType != Word.Void)
            {
                HandleParseError($"Function {method.Lexeme} can only return void.", stop: false);
            }

            var end = Match(TType.RightParenthesis);

            return new EventHandler(method.Lexeme);
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

                case TType.MultiplyAssignment:
                case TType.DivideAssignment:
                    {
                        Match(currentToken.TokenType);
                        var binding = SourceCodeBinding(start, currentScanResult);
                        var operation = new Token(operand.TokenType == TType.MultiplyAssignment ? TType.Star : TType.Division);

                        assignedExpression = new BinaryOperation(assignee, Expression(), operation, binding);
                        ret = new Assignment((Variable)assignee, assignedExpression, binding);
                    }
                    break;

                case TType.PlusAssignment:
                case TType.MinusAssignment:
                    {
                        Match(currentToken.TokenType);
                        var binding = SourceCodeBinding(start, currentScanResult);

                        if (operand.TokenType == TType.PlusAssignment &&
                            EventHandlerHelper.IsEventHandler(assignee, _parseContext, _typeInferer))
                        {
                            ret = new EventHandlerSubscription(assignee as Variable, EventHandler(), SourceCodeBinding(start, lastScanResult));
                        }
                        else
                        { 
                            assignedExpression = new BinaryOperation(assignee, Expression(), operand, SourceCodeBinding(start, currentScanResult));
                            ret = new Assignment(
                                (Variable)assignee,
                                assignedExpression,
                                binding);
                        }
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
                ValidateNotConst(assignee);

                var assigneeType = _typeInferer.InferType(assignee, false, _parseContext);

                if (assigneeType != null)
                {
                    var assignedType = _typeInferer.InferType(assignedExpression, false, _parseContext);

                    if (assignedType != null && !CanAssign(assignedType, assigneeType))
                    {
                        HandleParseError(string.Format(MessageProvider.ExceptionImplicitConversion, assignedType.Name, assigneeVar.ReturnType.Name), stop: false);
                    }
                }
            }
            else if (ret is EventHandlerSubscription subscription)
            {
                var assigneeType = _typeInferer.InferType(assignee, false, _parseContext);

                EventHandlerHelper.ValidateEventHandler(this, _parseContext, assigneeType, subscription.EventHandler);
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

        internal void HandleParseError(Exception exception, bool showLine = false, bool stop = true)
        {
            string message = exception.Message;

            if (exception.InnerException != null)
            {
                message = exception.InnerException.Message;
            }

            HandleParseError(message, showLine, stop);
        }

        internal void HandleParseError(string s, bool showLine = false, bool stop = true)
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
                System.Type callerType = isConstructor ? _constructorType : null;

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

        internal void HandleAutocompletionForType(string typeName)
        {
            if (!_forAutoCompletion) return;

            throw new AutoCompletionTypeInterruption(_typeInferer.InferType(typeName));
        }

        private bool IsAtAutocompletionPosition(IScanResult scanResult)
        {
            return _forAutoCompletion &&
                scanResult.Line == _stopAtRow &&
                scanResult.Start <= _stopAtColumn &&
                scanResult.End >= _stopAtColumn;
        }

        private void HandleNamespaceAutocompletion(string name)
        {
            if (IsAtAutocompletionPosition(lastScanResult))
            {
                throw new AutoCompletionTypeInterruption(ApplyUsingAlias(name));
            }
        }

        /// <summary>
        /// The dot in <c>System.IO.</c> at the start of an expression. There is nothing after it, so it would be a syntax error.
        /// </summary>
        private void HandleDottedNameAutocompletion()
        {
            if (!_forAutoCompletion) return;

            string name = ((Word)currentToken).Lexeme;

            if (_parseContext.CurrentScope.FindVariableDeclaration(name) != null) return;

            for (int offset = 0; PeekToken(offset).TokenType == TType.Dot; offset += 2)
            {
                string @namespace = ApplyUsingAlias(name);

                if (IsAtAutocompletionPosition(_lexer.Peek(offset)) && _proxy.ClrTypes.IsNamespace(@namespace))
                {
                    throw new AutoCompletionTypeInterruption(@namespace);
                }

                if (!(PeekToken(offset + 1) is Word)) return;

                name += "." + WordText(_lexer.Peek(offset + 1));
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
