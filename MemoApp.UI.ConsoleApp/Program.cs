using MemoApp.UI.ConsoleApp;

try
{
    ConsoleInterface.RunMainLoop();
}
catch (Exception ex)
{
    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey(true);
}
