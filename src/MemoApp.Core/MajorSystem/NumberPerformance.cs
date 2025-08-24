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
}