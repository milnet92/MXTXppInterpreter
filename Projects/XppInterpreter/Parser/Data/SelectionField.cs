using System;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser.Data
{
    public enum AggregateFunction
    {
        None,
        Sum,
        MaxOf,
        MinOf,
        Avg,
        Count
    }

    public class SelectionField
    {
        public AggregateFunction AggregateFunction { get; }
        public Field Field { get; }

        public SelectionField(Field field, AggregateFunction aggregateFunction = AggregateFunction.None)
        {
            Field = field;
            AggregateFunction = aggregateFunction;
        }

        public SelectionField(Field field, TType ttypeAggregateFunction)
        {
            Field = field;

            switch (ttypeAggregateFunction)
            {
                case TType.Sum:
                    AggregateFunction = AggregateFunction.Sum;
                    break;

                case TType.Avg:
                    AggregateFunction = AggregateFunction.Avg;
                    break;

                case TType.Count:
                    AggregateFunction = AggregateFunction.Count;
                    break;

                case TType.MaxOf:
                    AggregateFunction = AggregateFunction.MaxOf;
                    break;

                case TType.MinOf:
                    AggregateFunction = AggregateFunction.MinOf;
                    break;

                default:
                    throw new Exception($"Invalid aggregate function {ttypeAggregateFunction}.");
            }
        }
    }
}
