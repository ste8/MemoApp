using System.Text.Json.Serialization;

namespace MemoApp.Core.MajorSystem;

public readonly record struct NumberPerformance
{
    public MajorNumber Number { get; }
    public TimeSpan ResponseTime { get; }
    public DateTime PresentedAt { get; }
    public DateTime RespondedAt { get; }

    public NumberPerformance(MajorNumber number, DateTime presentedAt, DateTime respondedAt)
    {
        if (respondedAt < presentedAt)
            throw new ArgumentException("Response time cannot be before presentation time");

        Number = number;
        PresentedAt = presentedAt;
        RespondedAt = respondedAt;
        ResponseTime = respondedAt - presentedAt;
    }

    /// <summary>
    /// JSON constructor for deserialization.
    /// When deserializing from JSON, we need to ensure ResponseTime is properly calculated.
    /// </summary>
    [JsonConstructor]
    public NumberPerformance(MajorNumber number, TimeSpan responseTime, DateTime presentedAt, DateTime respondedAt)
    {
        Number = number;
        PresentedAt = presentedAt;
        RespondedAt = respondedAt;

        // For JSON deserialization, use the provided ResponseTime if it's valid,
        // otherwise calculate it from the timestamps
        if (responseTime != TimeSpan.Zero)
        {
            ResponseTime = responseTime;
        }
        else if (presentedAt != default && respondedAt != default)
        {
            ResponseTime = respondedAt - presentedAt;
        }
        else
        {
            ResponseTime = TimeSpan.Zero;
        }
    }
}