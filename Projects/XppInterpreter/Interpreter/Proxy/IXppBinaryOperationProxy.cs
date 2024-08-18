namespace XppInterpreter.Interpreter.Proxy
{
    public interface IXppBinaryOperationProxy
    {
        object Add(object left, object right);
        object Substract(object left, object right);
        object Multiply(object left, object right);
        object Divide(object left, object right);
        object IntDivide(object left, object right);
        object LeftShift(object left, object right);
        object RightShift(object left, object right);
        object BinaryAnd(object left, object right);
        object BinaryOr(object left, object right);
        object BinaryXOr(object left, object right);
        object Mod(object left, object right);
        bool Greater(object left, object right);
        bool AreEqual(object left, object right);
    }
}
