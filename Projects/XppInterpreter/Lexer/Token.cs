using System.Collections.Generic;
using System.Linq;

namespace XppInterpreter.Lexer
{
    /// <summary>
    /// Base class for a token
    /// </summary>
    public class Token
    {
        public readonly TType TokenType;

        public Token(TType tokenType)
        {
            TokenType = tokenType;
        }

        public override string ToString()
        {
            return $"{TagHelper.GetChar(TokenType).ToString()} [{TokenType}]";
        }


        public static class TagHelper
        {
            private static readonly Dictionary<char, TType> Char2TokenType = new Dictionary<char, TType>()
            {
                { '(', TType.LeftParenthesis },
                { ')', TType.RightParenthesis },
                { '{', TType.LeftBrace },
                { '}', TType.RightBrace },
                { '*', TType.Star },
                { '+', TType.Plus },
                { '-', TType.Minus },
                { '<', TType.Smaller },
                { '>', TType.Greater },
                { ';', TType.Semicolon },
                { '/', TType.Division },
                { ',', TType.Comma },
                { '.', TType.Dot },
                { '[', TType.LeftBracket },
                { ']', TType.RightBracket },
                { ':', TType.Colon },
                { '=', TType.Assign },
                { '?', TType.QuestionMark },
                { '^', TType.BinaryXOr},
            };

            public static char GetChar(TType tokenType)
            {
                if (Char2TokenType.ContainsValue(tokenType))
                {
                    return Char2TokenType.Where(v => v.Value == tokenType).First().Key;
                }

                return char.MaxValue;
            }

            public static TType GetTokenType(char c)
            {
                if (Char2TokenType.ContainsKey(c))
                {
                    return Char2TokenType[c];
                }

                return TType.Invalid;
            }
        }
    }

    public enum TType
    {
        New,
        Invalid,
        And,
        Assign,
        Or,
        False,
        SmallerOrEqual,
        GreaterOrEqual,
        Greater,
        Smaller,
        Equal,
        NotEqual,
        Negation,
        Real,
        Id,
        True,
        Else,
        If,
        Int32,
        Int64,
        Container,
        Date,
        Long,
        Star,
        Division,
        IntegerDivision,
        Mod,
        Minus,
        Plus,
        Command,
        CommandArg,
        CommandVerb,
        CommandValue,
        Semicolon,
        Comma,
        LeftParenthesis,
        RightParenthesis,
        BinaryOr,
        BinaryAnd,
        BinaryXOr,
        LeftShift,
        RightShift,
        LeftBrace,
        RightBrace,
        String,
        Comment,
        While,
        Do,
        For,
        Break,
        Breakpoint,
        Continue,
        Null,
        Dot,
        DoubleDot,
        StaticDoubleDot,
        Var,
        LeftBracket,
        RightBracket,
        QuestionMark,
        Colon,
        Try,
        Catch,
        Finally,
        Throw,
        Retry,
        Using,
        PlusAssignment,
        MinusAssignment,
        Increment,
        Decrement,
        Switch,
        Case,
        Default,
        Print,
        Is,
        As,
        Namespace,

        #region Type identification
        TypeAnytype,
        TypeEnum,
        TypeGuid,
        TypeTimeOfDay,
        TypeStr,
        TypeInt32,
        TypeInt64,
        TypeReal,
        TypeBoolean,
        TypeDatetime,
        TypeContainer,
        TypeDate,
        #endregion

        #region Query tokens
        TtsBegin,
        TtsCommit,
        TtsAbort,

        Select,
        From,
        Next,
        Where,
        Exists,
        NotExists,
        Join,
        Outer,
        On,
        Order,
        Desc,
        Asc,
        Group,
        By,
        MaxOf,
        MinOf,
        Sum,
        Count,
        Avg,
        Index,
        In,
        Like,

        UpdateRecordset,
        InsertRecordset,
        DeleteFrom,
        Setting,

        FirstFast,
        FirstOnly,
        FirstOnly10,
        FirstOnly100,
        FirstOnly1000,
        ForceLiterals,
        ForceNestedLoops,
        ForcePlaceHolders,
        ForceSelectOrder,
        ForUpdate,
        NoFetch,
        OptimisticLock,
        PessimisticLock,
        RepeatableRead,
        Reverse,
        ValidTimeState,
        ChangeCompany,
        CrossCompany,
        GenerateOnly,
        #endregion

        #region Functions
        Void,
        Return,
        #endregion

        EOF
    }
}
