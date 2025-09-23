using System.Collections.Generic;
using System.Linq;

namespace MemoApp.Core.MajorSystem.Models;

/// <summary>
/// Represents a digit-to-letter mapping in the Major System with localized content.
/// Contains the digit, associated letters/sounds, and practical examples.
/// </summary>
public class DigitLetterMapping
{
    /// <summary>
    /// The digit (0-9) this mapping represents
    /// </summary>
    public int Digit { get; init; }

    /// <summary>
    /// The letters/sounds associated with this digit (e.g., "S, soft C, Z")
    /// </summary>
    public string Letters { get; init; } = string.Empty;

    /// <summary>
    /// Practical examples of words using these letters (e.g., "Sue, Ace, Zoe")
    /// </summary>
    public string Examples { get; init; } = string.Empty;


    public DigitLetterMapping(int digit, string letters, string examples)
    {
        Digit = digit;
        Letters = letters;
        Examples = examples;
    }
}

/// <summary>
/// Service providing localized digit-letter mappings for the Major System.
/// Maintains consistency across the application for onboarding and help systems.
/// </summary>
public interface IDigitLetterMappingService
{
    /// <summary>
    /// Gets the complete set of digit-letter mappings for the current culture
    /// </summary>
    IReadOnlyList<DigitLetterMapping> GetMappings();

    /// <summary>
    /// Gets the mapping for a specific digit in the current culture
    /// </summary>
    DigitLetterMapping? GetMappingForDigit(int digit);

    /// <summary>
    /// Gets mappings for a specific culture/language
    /// </summary>
    IReadOnlyList<DigitLetterMapping> GetMappingsForCulture(string cultureCode);
}

/// <summary>
/// Implementation of digit-letter mapping service with support for English and Italian
/// </summary>
public class DigitLetterMappingService : IDigitLetterMappingService
{
    private readonly Dictionary<string, List<DigitLetterMapping>> _mappingsByCulture;

    public DigitLetterMappingService()
    {
        _mappingsByCulture = new Dictionary<string, List<DigitLetterMapping>>
        {
            ["en"] = CreateEnglishMappings(),
            ["it"] = CreateItalianMappings()
        };
    }

    public IReadOnlyList<DigitLetterMapping> GetMappings()
    {
        var currentCulture = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        return GetMappingsForCulture(currentCulture);
    }

    public DigitLetterMapping? GetMappingForDigit(int digit)
    {
        var mappings = GetMappings();
        return mappings.FirstOrDefault(m => m.Digit == digit);
    }

    public IReadOnlyList<DigitLetterMapping> GetMappingsForCulture(string cultureCode)
    {
        if (_mappingsByCulture.TryGetValue(cultureCode, out var mappings))
        {
            return mappings.AsReadOnly();
        }

        // Default to English if culture not found
        return _mappingsByCulture["en"].AsReadOnly();
    }

    private static List<DigitLetterMapping> CreateEnglishMappings()
    {
        return new List<DigitLetterMapping>
        {
            new(0, "S, soft C, Z", "Sue, Ace, Zoe"),
            new(1, "T, D, TH (both in thing and this)", "Tea, Doe, Thigh, They"),
            new(2, "N", "No"),
            new(3, "M", "Moo"),
            new(4, "R", "Ray"),
            new(5, "L", "Lee"),
            new(6, "CH (in cheese and chef), J, soft G, SH", "Chew, Cha, Jaw, Joe, She"),
            new(7, "K, hard C, Q, hard G, CH (as in loch)", "Key, Co, Q, Go, Loch"),
            new(8, "F, PH, V", "Fee, Pho, Vee"),
            new(9, "P, B", "Pee, Bee")
        };
    }

    private static List<DigitLetterMapping> CreateItalianMappings()
    {
        return new List<DigitLetterMapping>
        {
            new(0, "S, SC, Z", "sì, asso, sci, ascia, zio, zoo"),
            new(1, "T, D", "tè, dea"),
            new(2, "N, GN", "neo, anno, gnu"),
            new(3, "M", "amo, mio"),
            new(4, "R", "re, oro"),
            new(5, "L, GL", "ali, aglio"),
            new(6, "C, G dolci", "ciao, ace, oggi"),
            new(7, "C, G dure, K", "occhio, eco, qui, ago"),
            new(8, "F, V", "ufo, uva, via"),
            new(9, "P, B", "boa, ape")
        };
    }
}