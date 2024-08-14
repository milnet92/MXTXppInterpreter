using System;

namespace XppInterpreter.Lexer
{
    /// <summary>
    /// Represents a token composed by multiple letters
    /// </summary>
    public class Word : Token, IComparable<Word>, IComparable<string>
    {
        public readonly string Lexeme;

        #region Binary operations
        public static readonly Word And = new Word("&&", TType.And);
        public static readonly Word Or = new Word("||", TType.Or);
        public static readonly Word IntegerDivision = new Word("div", TType.IntegerDivision);
        public static readonly Word Mod = new Word("mod", TType.Mod);
        public static readonly Word Equal = new Word("==", TType.Equal);
        public static readonly Word NotEqual = new Word("!=", TType.NotEqual);
        public static readonly Word LessOrEqual = new Word("<=", TType.SmallerOrEqual);
        public static readonly Word GreaterOrEqual = new Word(">=", TType.GreaterOrEqual);
        public static readonly Word PlusAssignment = new Word("+=", TType.PlusAssignment);
        public static readonly Word MinusAssignment = new Word("-=", TType.MinusAssignment);
        public static readonly Word Increment = new Word("++", TType.Increment);
        public static readonly Word Decrement = new Word("--", TType.GreaterOrEqual);
        #endregion

        #region Misc tokens
        public static readonly Word StaticDoubleDot = new Word("::", TType.StaticDoubleDot);
        public static readonly Word True = new Word("true", TType.True);
        public static readonly Word False = new Word("false", TType.False);
        public static readonly Word If = new Word("if", TType.If);
        public static readonly Word Else = new Word("else", TType.Else);
        public static readonly Word While = new Word("while", TType.While);
        public static readonly Word Do = new Word("do", TType.Do);
        public static readonly Word For = new Word("for", TType.For);
        public static readonly Word Break = new Word("break", TType.Break);
        public static readonly Word Continue = new Word("continue", TType.Continue);
        public static readonly Word Breakpoint = new Word("breakpoint", TType.Breakpoint);
        public static readonly Word Null = new Word("null", TType.Null);
        public static readonly Word Throw = new Word("throw", TType.Throw);
        public static readonly Word Try = new Word("try", TType.Try);
        public static readonly Word Catch = new Word("catch", TType.Catch);
        public static readonly Word Finally = new Word("finally", TType.Finally);
        public static readonly Word Switch = new Word("switch", TType.Switch);
        public static readonly Word Default = new Word("default", TType.Default);
        public static readonly Word Case = new Word("case", TType.Case);
        public static readonly Word New = new Word("new", TType.New);
        public static readonly Word Print = new Word("print", TType.Print);
        public static readonly Word Is = new Word("is", TType.Is);
        public static readonly Word As = new Word("as", TType.As);
        public static readonly Word Using = new Word("using", TType.Using);
        #endregion

        #region Data and query tokens
        public static readonly Word TtsBegin = new Word("ttsbegin", TType.TtsBegin);
        public static readonly Word TtsCommit = new Word("ttscommit", TType.TtsCommit);
        public static readonly Word TtsAbort = new Word("ttsabort", TType.TtsAbort);
        public static readonly Word Select = new Word("select", TType.Select);
        public static readonly Word From = new Word("from", TType.From);
        public static readonly Word Next = new Word("next", TType.Next);
        public static readonly Word Where = new Word("where", TType.Where);
        public static readonly Word Exists = new Word("exists", TType.Exists);
        public static readonly Word NotExists = new Word("notexists", TType.NotExists);
        public static readonly Word Join = new Word("join", TType.Join);
        public static readonly Word Outer = new Word("outer", TType.Outer);
        public static readonly Word On = new Word("on", TType.On);
        public static readonly Word Group = new Word("group", TType.Group);
        public static readonly Word Order = new Word("order", TType.Order);
        public static readonly Word By = new Word("by", TType.By);
        public static readonly Word Desc = new Word("desc", TType.Desc);
        public static readonly Word Asc = new Word("asc", TType.Asc);
        public static readonly Word Sum = new Word("sum", TType.Sum);
        public static readonly Word Avg = new Word("avg", TType.Avg);
        public static readonly Word MaxOf = new Word("maxOf", TType.MaxOf);
        public static readonly Word MinOf = new Word("minOf", TType.MinOf);
        public static readonly Word Count = new Word("count", TType.Count);
        public static readonly Word UpdateRecordset = new Word("update_recordset", TType.UpdateRecordset);
        public static readonly Word InsertRecordset = new Word("insert_recordset", TType.InsertRecordset);
        public static readonly Word DeleteFrom = new Word("delete_from", TType.DeleteFrom);
        public static readonly Word Setting = new Word("setting", TType.Setting);
        public static readonly Word FirstFast = new Word("firstFast", TType.FirstFast);
        public static readonly Word FirstOnly = new Word("firstOnly", TType.FirstOnly);
        public static readonly Word FirstOnly10 = new Word("firstOnly10", TType.FirstOnly10);
        public static readonly Word FirstOnly100 = new Word("firstOnly100", TType.FirstOnly100);
        public static readonly Word FirstOnly1000 = new Word("firstOnly1000", TType.FirstOnly1000);
        public static readonly Word ForceLiterals = new Word("forceLiterals", TType.ForceLiterals);
        public static readonly Word ForceNestedLoops = new Word("forceNestedLoop", TType.ForceNestedLoops);
        public static readonly Word ForceSelectOrder = new Word("forceSelectOrder", TType.ForceSelectOrder);
        public static readonly Word ForcePlaceHolders = new Word("forcePlaceHolders", TType.ForcePlaceHolders);
        public static readonly Word ForUpdate = new Word("forUpdate", TType.ForUpdate);
        public static readonly Word NoFetch = new Word("noFetch", TType.NoFetch);
        public static readonly Word OptimisticLock = new Word("optimisticLock", TType.OptimisticLock);
        public static readonly Word PessimisticLock = new Word("pessimisticLock", TType.PessimisticLock);
        public static readonly Word RepeatableRead = new Word("repeatableRead", TType.RepeatableRead);
        public static readonly Word Reverse = new Word("reverse", TType.Reverse);
        public static readonly Word ValidTimeState = new Word("validTimeState", TType.ValidTimeState);
        public static readonly Word ChangeCompany = new Word("changeCompany", TType.ChangeCompany);
        public static readonly Word CrossCompany = new Word("crossCompany", TType.CrossCompany);
        public static readonly Word GenerateOnly = new Word("generateOnly", TType.GenerateOnly);
        public static readonly Word Index = new Word("index", TType.Index);
        public static readonly Word In = new Word("in", TType.In);
        public static readonly Word Like = new Word("like", TType.Like);
        #endregion

        #region Function tokens
        public static readonly Word Void = new Word("void", TType.Void);
        public static readonly Word Return = new Word("return", TType.Return);
        #endregion

        public Word(string lexeme, TType tokenType) : base(tokenType)
        {
            Lexeme = lexeme;
        }

        public override string ToString()
        {
            return $"{Lexeme} [{this.TokenType}]";
        }

        public int CompareTo(Word other)
        {
            if (other is null)
            {
                return 0;
            }

            return string.Compare(Lexeme, other.Lexeme, true);
        }

        public int CompareTo(string other)
        {
            return string.Compare(Lexeme, other, true);
        }
    }
}
