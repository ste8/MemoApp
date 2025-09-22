using System.Text.Json.Serialization;

namespace MemoApp.Core.MajorSystem;

public readonly record struct SessionStatistics
{
    public TimeSpan TotalDuration { get; }
    public TimeSpan AverageResponseTime { get; }
    public int TotalNumbers { get; }
    public NumberPerformance FastestResponse { get; }
    public NumberPerformance SlowestResponse { get; }
    public IReadOnlyList<NumberPerformance> SlowestResponses { get; }
    public IReadOnlyList<NumberPerformance> AllPerformances { get; }
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }

    /// <summary>
    /// JSON constructor for deserialization.
    /// </summary>
    [JsonConstructor]
    public SessionStatistics(
        TimeSpan totalDuration,
        TimeSpan averageResponseTime,
        int totalNumbers,
        NumberPerformance fastestResponse,
        NumberPerformance slowestResponse,
        IReadOnlyList<NumberPerformance> slowestResponses,
        IReadOnlyList<NumberPerformance> allPerformances,
        DateTime startTime,
        DateTime endTime)
    {
        TotalDuration = totalDuration;
        AverageResponseTime = averageResponseTime;
        TotalNumbers = totalNumbers;
        FastestResponse = fastestResponse;
        SlowestResponse = slowestResponse;
        SlowestResponses = slowestResponses;
        AllPerformances = allPerformances;
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>
    /// Standard constructor for creating statistics from performance data.
    /// </summary>
    public SessionStatistics(
        IEnumerable<NumberPerformance> performances,
        DateTime startTime,
        DateTime endTime)
    {
        var performanceList = performances.ToList();
        
        if (!performanceList.Any())
            throw new ArgumentException("Cannot create statistics from empty performance list");

        StartTime = startTime;
        EndTime = endTime;
        TotalDuration = endTime - startTime;
        TotalNumbers = performanceList.Count;
        
        AverageResponseTime = TimeSpan.FromMilliseconds(
            performanceList.Average(p => p.ResponseTime.TotalMilliseconds));
        
        FastestResponse = performanceList.MinBy(p => p.ResponseTime);
        SlowestResponse = performanceList.MaxBy(p => p.ResponseTime);
        
        SlowestResponses = performanceList
            .OrderByDescending(p => p.ResponseTime)
            .Take(Math.Min(10, performanceList.Count))
            .ToList()
            .AsReadOnly();
        
        AllPerformances = performanceList
            .OrderBy(p => p.Number)
            .ToList()
            .AsReadOnly();
    }
}