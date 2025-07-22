using MemoApp.Core.MajorSystem;

namespace MemoApp.UI.ConsoleApp;

public static class ConsoleInterface
{
    public static void RunMainLoop()
    {
        Console.WriteLine("=== Major System Training App ===");
        Console.WriteLine();
        
        while (true)
        {
            ShowMainMenu();
            var choice = Console.ReadKey(true).KeyChar;
            Console.WriteLine();
            
            switch (choice)
            {
                case '1':
                    RunTrainingSession();
                    break;
                case '2':
                    RunCustomRange();
                    break;
                case '3':
                    ShowMajorSystemHelp();
                    break;
                case 'q':
                case 'Q':
                    Console.WriteLine("Thanks for training! Goodbye!");
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
            
            Console.WriteLine();
        }
    }

    private static void ShowMainMenu()
    {
        Console.WriteLine("Choose an option:");
        Console.WriteLine("1. Quick Training (00-09)");
        Console.WriteLine("2. Custom Range");
        Console.WriteLine("3. Major System Help");
        Console.WriteLine("Q. Quit");
        Console.Write("Your choice: ");
    }

    private static void RunTrainingSession(string rangeStart = "00", string rangeEnd = "09")
    {
        Console.Clear();
        Console.WriteLine($"=== Training Session: {rangeStart} to {rangeEnd} ===");
        Console.WriteLine();

        try
        {
            var session = new GameSession(rangeStart, rangeEnd);
            session.StartSession();

            Console.WriteLine($"Starting session with {session.TotalNumbers} numbers.");
            Console.WriteLine("For each number, recall your major system word/image, then press ENTER...");
            Console.WriteLine("Press CTRL+C to abort the session.");
            Console.WriteLine();

            while (session.State == SessionState.InProgress)
            {
                Console.WriteLine($"Number: {session.CurrentNumber}");
                Console.WriteLine($"Progress: {session.CompletedNumbers}/{session.TotalNumbers}");
                Console.Write("Press ENTER when ready for next number...");
                Console.ReadLine();
                session.AdvanceToNext();
                Console.WriteLine();
            }

            ShowSessionResults(session);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during training session: {ex.Message}");
        }
        
        Console.WriteLine("Press any key to return to main menu...");
        Console.ReadKey(true);
    }

    private static void RunCustomRange()
    {
        Console.Clear();
        Console.WriteLine("=== Custom Range Training ===");
        Console.WriteLine();
        
        try
        {
            Console.Write("Enter start number (e.g., '00', '0', '25'): ");
            var startRange = Console.ReadLine()?.Trim();
            
            Console.Write("Enter end number (e.g., '09', '50', '99'): ");
            var endRange = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrWhiteSpace(startRange) || string.IsNullOrWhiteSpace(endRange))
            {
                Console.WriteLine("Invalid range. Please enter valid numbers.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
                return;
            }
            
            Console.WriteLine();
            RunTrainingSession(startRange, endRange);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Invalid range: {ex.Message}");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }

    private static void ShowSessionResults(GameSession session)
    {
        var stats = session.GetStatistics();
        
        Console.WriteLine("=== Session Complete! ===");
        Console.WriteLine($"Total Duration: {stats.TotalDuration:mm\\:ss}");
        Console.WriteLine($"Average Response Time: {stats.AverageResponseTime:ss\\.ff}s");
        Console.WriteLine($"Fastest Recall: {stats.FastestResponse.Number} ({stats.FastestResponse.ResponseTime:ss\\.ff}s)");
        Console.WriteLine($"Slowest Recall: {stats.SlowestResponse.Number} ({stats.SlowestResponse.ResponseTime:ss\\.ff}s)");
        
        Console.WriteLine();
        Console.WriteLine("Numbers that took longest to recall:");
        var slowestCount = Math.Min(5, stats.SlowestResponses.Count);
        for (int i = 0; i < slowestCount; i++)
        {
            var perf = stats.SlowestResponses[i];
            Console.WriteLine($"  {i + 1}. {perf.Number}: {perf.ResponseTime:ss\\.ff}s");
        }
        Console.WriteLine();
    }

    private static void ShowMajorSystemHelp()
    {
        Console.Clear();
        Console.WriteLine("=== Major System Quick Reference ===");
        Console.WriteLine();
        Console.WriteLine("The Major System converts numbers to memorable words using consonant sounds:");
        Console.WriteLine();
        Console.WriteLine("0 → S, soft C, Z sounds    |  5 → L sound");
        Console.WriteLine("1 → T, D sounds            |  6 → J, SH, CH, soft G sounds");
        Console.WriteLine("2 → N sound                |  7 → K, hard C, hard G sounds");
        Console.WriteLine("3 → M sound                |  8 → F, V sounds");
        Console.WriteLine("4 → R sound                |  9 → P, B sounds");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  42 → R + N sounds → 'Rain', 'Ran', 'Ruin'");
        Console.WriteLine("  07 → S + K sounds → 'Sock', 'Sack'");
        Console.WriteLine("  25 → N + L sounds → 'Nail', 'Nile'");
        Console.WriteLine();
        Console.WriteLine("Tips:");
        Console.WriteLine("- Vowels (A, E, I, O, U) don't count - use them freely");
        Console.WriteLine("- Create vivid, memorable images for each number");
        Console.WriteLine("- Practice regularly to build strong associations");
        Console.WriteLine();
        Console.WriteLine("Press any key to return to main menu...");
        Console.ReadKey(true);
    }
}