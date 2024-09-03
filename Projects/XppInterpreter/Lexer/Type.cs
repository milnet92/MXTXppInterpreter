namespace XppInterpreter.Lexer
{
    /// <summary>
    /// Represents a token that is used for type reference
    /// </summary>
    public class Type : Word
    {
        public static readonly Type Int32 = new Type("int", TType.TypeInt32);
        public static readonly Type Int64 = new Type("int64", TType.TypeInt64);
        public static readonly Type Anytype = new Type("anytype", TType.TypeAnytype);
        public static readonly Type Container = new Type("container", TType.TypeContainer);
        public static readonly Type Real = new Type("real", TType.TypeReal);
        public static readonly Type DateTime = new Type("utcdatetime", TType.TypeDatetime);
        public static readonly Type Date = new Type("date", TType.TypeDate);
        public static readonly Type TimeOfDay = new Type("timeofday", TType.TypeTimeOfDay);
        public static readonly Type Str = new Type("str", TType.TypeStr);
        public static readonly Type Var = new Type("var", TType.Var);

        public Type(string lexeme, TType tokenType) : base(lexeme, tokenType)
        {

        }
    }
}
