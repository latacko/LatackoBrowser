namespace Units;
[Flags]
public enum SetTypes : byte
{
    None = 0,
    X = 1 << 0,
    Y = 1 << 1,
    Z = 1 << 2,
    W = 1 << 3,
}