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
        // Load digit-to-sound mappings
        Mappings = new ObservableCollection<MajorSystemMapping>
        {
            new("0", "S, soft C, Z", "Sounds like 'zero'", "🔄"),
            new("1", "T, D", "One downstroke", "👆"),
            new("2", "N", "Two downstrokes", "✌️"),
            new("3", "M", "Three downstrokes", "🤟"),
            new("4", "R", "Last letter of 'four'", "4️⃣"),
            new("5", "L", "Roman numeral L = 50", "🖐"),
            new("6", "J, SH, CH, soft G", "J looks like reversed 6", "6️⃣"),
            new("7", "K, hard C, hard G", "K has two 7s", "7️⃣"),
            new("8", "F, V", "F looks like 8", "8️⃣"),
            new("9", "P, B", "P looks like reversed 9", "9️⃣")
        };

        // Load example words
        Examples = new ObservableCollection<ExampleWord>
        {
            new("42", "Rain", "R (4) + N (2) = Rain", "🌧️"),
            new("25", "Nail", "N (2) + L (5) = Nail", "🔨"),
            new("07", "Sock", "S (0) + K (7) = Sock", "🧦"),
            new("18", "Dove", "D (1) + V (8) = Dove", "🕊️"),
            new("63", "Gym", "J (6) + M (3) = Gym", "💪"),
            new("91", "Bat", "B (9) + T (1) = Bat", "🦇"),
            new("50", "Lace", "L (5) + S (0) = Lace", "🎀"),
            new("34", "Mare", "M (3) + R (4) = Mare", "🐎"),
            new("76", "Cage", "K (7) + J (6) = Cage", "🔒"),
            new("89", "Fob", "F (8) + B (9) = Fob", "🔑")
        };
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