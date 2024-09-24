namespace XppInterpreter.Lexer
{
    /// <summary>
    /// Represents a token related to a type (int, string, etc.) used
    /// or constants
    /// </summary>
    public class BaseType : Token
    {
        public object Value { get; }

        public BaseType(object value, TType tokenType) : base(tokenType)
        {
            Value = value;
        }
        public override string ToString()
        {
            return $"{Value} [{TokenType}]";
        }
    }

    /// <summary>
    /// String base type token
    /// </summary>
    public class String : BaseType
    {
        public String(string value) : base(value, TType.String) { }
    }

    /// <summary>
    /// Int32 (int) base type token
    /// </summary>
    public class Int32 : BaseType
    {
        public Int32(int value) : base(value, TType.Int32) { }
    }

    /// <summary>
    /// Int64 (long) base type token
    /// </summary>
    public class Int64 : BaseType
    {
        public Int64(long value) : base(value, TType.Int64) { }
    }

    /// <summary>
    /// Real (decimal) base type token
    /// </summary>
    public class Real : BaseType
    {
        public Real(decimal value) : base(value, TType.Real) { }
    }

    /// <summary>
    /// Container base type token
    /// </summary>
    public class Container : BaseType
    {
        public Container(object[] container) : base(container, TType.Container) { }
    }


    /// <summary>
    /// Date base type token
    /// </summary>
    public class Date : BaseType
    {
        public Date(DateLiteral date) : base(date, TType.Date) { }
    }

    public readonly struct DateLiteral
    {
        public int Day { get; }
        public int Month { get; }
        public int Year { get; }

        public DateLiteral(int day, int month, int year)
        {
            Day = day;
            Month = month;
            Year = year;
        }
    }
}
