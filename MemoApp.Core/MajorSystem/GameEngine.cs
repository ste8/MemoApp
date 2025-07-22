namespace MemoApp.Core.MajorSystem;

public class GameEngine
{
    public static GameSession CreateSession(string rangeStart, string rangeEnd)
    {
        return new GameSession(rangeStart, rangeEnd);
    }

    public static void RunConsoleDemo(string rangeStart = "00", string rangeEnd = "09")
    {
        Console.WriteLine($"=== Major System Training Demo ===");
        Console.WriteLine($"Range: {rangeStart} to {rangeEnd}");
        Console.WriteLine();

        var session = CreateSession(rangeStart, rangeEnd);
        session.StartSession();

        Console.WriteLine($"Starting session with {session.TotalNumbers} numbers.");
        Console.WriteLine("Press ENTER after recalling each number...");
        Console.WriteLine();

        while (session.State == SessionState.InProgress)
        {
            Console.WriteLine($"Number: {session.CurrentNumber}");
            Console.WriteLine($"Progress: {session.CompletedNumbers}/{session.TotalNumbers}");
            Console.ReadLine(); // Wait for user input
            session.AdvanceToNext();
            Console.WriteLine();
        }

        var stats = session.GetStatistics();
        Console.WriteLine("=== Session Complete ===");
        Console.WriteLine($"Total Duration: {stats.TotalDuration:mm\\:ss}");
        Console.WriteLine($"Average Response Time: {stats.AverageResponseTime:ss\\.ff}s");
        Console.WriteLine($"Fastest: {stats.FastestResponse.Number} ({stats.FastestResponse.ResponseTime:ss\\.ff}s)");
        Console.WriteLine($"Slowest: {stats.SlowestResponse.Number} ({stats.SlowestResponse.ResponseTime:ss\\.ff}s)");
        
        Console.WriteLine();
        Console.WriteLine("Top 5 Slowest Numbers:");
        foreach (var perf in stats.SlowestResponses.Take(5))
        {
            Console.WriteLine($"  {perf.Number}: {perf.ResponseTime:ss\\.ff}s");
        }
    }
}