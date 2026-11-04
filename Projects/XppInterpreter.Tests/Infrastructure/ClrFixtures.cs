using System;
using System.Collections;
using System.Collections.Generic;

namespace ClrFixtures
{
    /// <summary>Shows which overload was chosen.</summary>
    public static class Overloads
    {
        public static string Number(int value) => "int";
        public static string Number(long value) => "long";
        public static string Number(decimal value) => "decimal";
        public static string Number(double value) => "double";

        public static string WidenInt(long value) => "long";
        public static string WidenInt(object value) => "object";

        public static string OnlyDouble(double value) => "double " + value;
        public static string OnlyLong(long value) => "long " + value;
        public static string OnlyInt(int value) => "int " + value;
        public static string OnlyByte(byte value) => "byte " + value;
        public static string OnlyBool(bool value) => "bool " + value;
        public static string OnlyChar(char value) => "char " + value;
        public static string OnlyDayOfWeek(DayOfWeek value) => "day " + value;

        public static string StringOrObject(string value) => "string";
        public static string StringOrObject(object value) => "object";

        public static string ClassOrInterface(Stream2 value) => "class";
        public static string ClassOrInterface(IDisposable value) => "interface";

        public static string ToBase(Base value) => "base";
        public static string ToBase(object value) => "object";

        public static string Derived(Base value) => "base";
        public static string Derived(Derived value) => "derived";

        public static string Nulls(string value) => "string";
        public static string Nulls(StringBuilderLike value) => "builder";
        public static string Nulls(object value) => "object";

        public static string EnumOrInt(int value) => "int";
        public static string EnumOrObject(int value) => "int";
        public static string EnumOrObject(object value) => "object";

        public static string Optional(int a, string b = "default", int c = 7) => $"{a} {b} {c}";
        public static string OptionalOrExact(int a) => "exact";
        public static string OptionalOrExact(int a, int b = 1) => "optional";
        public static string OptionalStruct(int a, System.Threading.CancellationToken token = default) => $"{a} {token.CanBeCanceled}";

        public static string Params(params string[] values) => string.Join(",", values);
        public static string ParamsAfter(int first, params object[] values) => first + ":" + values.Length;
        public static string ParamsOrExact(string a, string b) => "exact";
        public static string ParamsOrExact(params string[] values) => "params";
        public static string ParamsChars(params char[] values) => new string(values);

        public static string NullableLong(long? value) => value.HasValue ? "value " + value : "null";
        public static string Struct(TestPoint point) => point.X + "," + point.Y;

        public static bool TryDouble(int value, out int result)
        {
            result = value * 2;
            return true;
        }

        public static void Swap(ref int a, ref string b)
        {
            a = a + 1;
            b = b + "!";
        }

        public static string Generic<T>(T value) => "generic";
        public static string Generic(string value) => "not generic";

        public static void Boom() => throw new InvalidOperationException("boom");
        public static void BoomIo() => throw new System.IO.FileNotFoundException("no file");
    }

    public class Stream2 : IDisposable
    {
        public void Dispose() { }
    }

    public class StringBuilderLike
    {
    }

    public class Base
    {
        public virtual string Who() => "base";
    }

    public class Derived : Base
    {
        public override string Who() => "derived";
        public string OnlyDerived() => "only derived";
    }

    public struct TestPoint
    {
        public int X { get; set; }
        public int Y;

        public TestPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public TestPoint Moved(int by) => new TestPoint(X + by, Y + by);
    }

    /// <summary>Like Azure.Response&lt;bool&gt;: a class with an implicit conversion to the value it holds.</summary>
    public class Response<T>
    {
        public T Value { get; }

        public Response(T value) { Value = value; }

        public T GetValue() => Value;

        public static implicit operator T(Response<T> response) => response.Value;
    }

    /// <summary>What Azure really returns: a private class derived from Response&lt;T&gt;.</summary>
    internal class ValueResponse<T> : Response<T>
    {
        public ValueResponse(T value) : base(value) { }
    }

    public class Client
    {
        public static int Created;

        public bool Disposed;

        public Client() { Created++; }

        public Response<bool> Exists(System.Threading.CancellationToken token) => new ValueResponse<bool>(false);
        public Response<bool> ReallyExists() => new ValueResponse<bool>(true);
        public Response<Item> Download() => new ValueResponse<Item>(new Item("file.txt"));

        /// <summary>Like Azure.Pageable: its enumerator is a private class generated by the compiler.</summary>
        public IEnumerable<Item> Items()
        {
            yield return new Item("a");
            yield return new Item("b");
        }

        public IEnumerator<Item> ItemsEnumerator() => Items().GetEnumerator();

        // Like ShareFileClient.Create: overloads told apart by the number of arguments, with null arguments of any class
        public string Create(long size, Item item, IDictionary<string, string> metadata, System.Threading.CancellationToken token) => "four";
        public string Create(long size, Item item, IDictionary<string, string> metadata, string permission, System.Threading.CancellationToken token) => "five";
    }

    public class Item
    {
        public string Name { get; }
        public bool IsDirectory => false;
        public long? FileSize => 5;
        public long? NoSize => null;
        public string PublicField = "field";

        public Item(string name) { Name = name; }
    }

    public class Range
    {
        public long Offset { get; }
        public long? Length { get; }

        public Range(long offset = 0, long? length = null)
        {
            Offset = offset;
            Length = length;
        }
    }

    public class Disposable : IDisposable
    {
        public static bool WasDisposed;

        public void Dispose() { WasDisposed = true; }

        public void Fail() => throw new InvalidOperationException("failed");
    }

    /// <summary>Implements Dispose explicitly, so the name is not public.</summary>
    public class ExplicitDisposable : IDisposable
    {
        public static bool WasDisposed;

        void IDisposable.Dispose() { WasDisposed = true; }
    }

    public static class Statics
    {
        public static int Number = 3;
        public static string Text { get; set; } = "text";
        public const int Constant = 9;
        public static long Long { get; set; }

        public static class Nested
        {
            public static string Name() => "nested";
        }

        public enum Kind
        {
            One,
            Two
        }
    }

    [Flags]
    public enum Options
    {
        None = 0,
        A = 1,
        B = 2
    }

    public class Members
    {
        public int Number;
        public long LongProperty { get; set; }
        public string ReadOnly => "read only";
        public double DoubleField;
        public string this[int index] => "indexed " + index;
        public DayOfWeek Day { get; set; }

        public string CaseDiffers() => "method";
    }

    public class CollectionLike : IEnumerable
    {
        public IEnumerator GetEnumerator() => new[] { 1, 2 }.GetEnumerator();
    }

    public class GenericHolder<T>
    {
        public T Value { get; set; }

        public string TypeName() => typeof(T).Name;

        public static string StaticName() => "static " + typeof(T).Name;
    }

    public class TwoArguments<A, B>
    {
        public string Names() => typeof(A).Name + "," + typeof(B).Name;
    }
}
