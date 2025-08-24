namespace MemoApp.Core.MajorSystem;

public readonly record struct MajorNumber : IComparable<MajorNumber>, IComparable
{
    public int Value { get; }
    public bool IsZeroPrefixed { get; }
    public string Display => IsZeroPrefixed ? Value.ToString("00") : Value.ToString();

    public MajorNumber(int value, bool isZeroPrefixed = false)
    {
        if (value < 0 || value > 99)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 99");
        
        if (isZeroPrefixed && value > 9)
            throw new ArgumentException("Zero-prefixed numbers can only be 0-9", nameof(isZeroPrefixed));

        Value = value;
        IsZeroPrefixed = isZeroPrefixed;
    }

    public int CompareTo(MajorNumber other)
    {
        return Value.CompareTo(other.Value);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is MajorNumber other) return CompareTo(other);
        throw new ArgumentException($"Object must be of type {nameof(MajorNumber)}");
    }

    public override string ToString() => Display;

    public static implicit operator int(MajorNumber number) => number.Value;
    
    public static MajorNumber FromValue(int value) => new(value, false);
    public static MajorNumber FromZeroPrefixed(int value) => new(value, true);
}