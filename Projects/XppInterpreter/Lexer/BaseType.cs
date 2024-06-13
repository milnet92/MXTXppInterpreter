using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class Real : BaseType
    {
        public Real(decimal value) : base(value, TType.Real) { }
    }

    public class Container : BaseType
    {
        public Container(object[] container) : base(container, TType.Container) { }
    }

    public class Date : BaseType
    {
        public Date(object date) : base(date, TType.Date) { }
    }
}
