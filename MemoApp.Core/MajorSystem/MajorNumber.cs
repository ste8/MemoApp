namespace MemoApp.Core.MajorSystem;

public readonly record struct MajorNumber
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

    public override string ToString() => Display;

    public static implicit operator int(MajorNumber number) => number.Value;
    
    public static MajorNumber FromValue(int value) => new(value, false);
    public static MajorNumber FromZeroPrefixed(int value) => new(value, true);
}