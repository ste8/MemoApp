using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the Major System help and reference page.
/// Provides comprehensive guidance on the Major System technique.
/// </summary>
public partial class HelpViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<MajorSystemMapping> mappings = new();

    [ObservableProperty]
    private ObservableCollection<ExampleWord> examples = new();

    public HelpViewModel()
    {
        Title = "Major System Guide";
        LoadMajorSystemData();
    }

    [RelayCommand]
    private async Task StartQuickTraining()
    {
        var parameters = new Dictionary<string, object>
        {
            { "rangeStart", "00" },
            { "rangeEnd", "09" }
        };
        
        await Shell.Current.GoToAsync("training", parameters);
    }

    [RelayCommand]
    private async Task GoToMainPage()
    {
        await Shell.Current.GoToAsync("//main");
    }

    private void LoadMajorSystemData()
    {
        // Clear existing collections to ensure proper binding
        Mappings.Clear();
        Examples.Clear();

        // Load digit-to-sound mappings
        var mappingData = new[]
        {
            new MajorSystemMapping("0", "S, soft C, Z", "Sounds like 'zero'", "🔄"),
            new MajorSystemMapping("1", "T, D", "One downstroke", "👆"),
            new MajorSystemMapping("2", "N", "Two downstrokes", "✌️"),
            new MajorSystemMapping("3", "M", "Three downstrokes", "🤟"),
            new MajorSystemMapping("4", "R", "Last letter of 'four'", "4️⃣"),
            new MajorSystemMapping("5", "L", "Roman numeral L = 50", "🖐"),
            new MajorSystemMapping("6", "J, SH, CH, soft G", "J looks like reversed 6", "6️⃣"),
            new MajorSystemMapping("7", "K, hard C, hard G", "K has two 7s", "7️⃣"),
            new MajorSystemMapping("8", "F, V", "F looks like 8", "8️⃣"),
            new MajorSystemMapping("9", "P, B", "P looks like reversed 9", "9️⃣")
        };

        // Load example words
        var exampleData = new[]
        {
            new ExampleWord("42", "Rain", "R (4) + N (2) = Rain", "🌧️"),
            new ExampleWord("25", "Nail", "N (2) + L (5) = Nail", "🔨"),
            new ExampleWord("07", "Sock", "S (0) + K (7) = Sock", "🧦"),
            new ExampleWord("18", "Dove", "D (1) + V (8) = Dove", "🕊️"),
            new ExampleWord("63", "Gym", "J (6) + M (3) = Gym", "💪"),
            new ExampleWord("91", "Bat", "B (9) + T (1) = Bat", "🦇"),
            new ExampleWord("50", "Lace", "L (5) + S (0) = Lace", "🎀"),
            new ExampleWord("34", "Mare", "M (3) + R (4) = Mare", "🐎"),
            new ExampleWord("76", "Cage", "K (7) + J (6) = Cage", "🔒"),
            new ExampleWord("89", "Fob", "F (8) + B (9) = Fob", "🔑")
        };

        // Add items individually to trigger proper change notifications
        foreach (var mapping in mappingData)
            Mappings.Add(mapping);

        foreach (var example in exampleData)
            Examples.Add(example);
    }
}

/// <summary>
/// Represents a digit-to-consonant sound mapping in the Major System.
/// </summary>
public record MajorSystemMapping(string Digit, string Consonants, string Memory, string Icon);

/// <summary>
/// Represents an example word created using the Major System.
/// </summary>
public record ExampleWord(string Number, string Word, string Explanation, string Icon);