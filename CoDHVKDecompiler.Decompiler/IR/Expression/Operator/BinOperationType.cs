namespace CoDHVKDecompiler.Decompiler.IR.Expression.Operator
{
    public enum BinOperationType : byte
    {
        OpAdd,
        OpSub,
        OpMul,
        OpDiv,
        OpMod,
        OpPow,
        OpEqual,
        OpNotEqual,
        OpLessThan,
        OpLessEqual,
        OpGreaterThan,
        OpGreaterEqual,
        OpAnd,
        OpOr,
        OpBAnd,
        OpBOr,
        OpShiftRight,
        OpShiftLeft,
        OpLoopCompare,
    }
}