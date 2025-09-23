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

    /// <summary>
    /// Memory aid or explanation for remembering this mapping
    /// </summary>
    public string MemoryAid { get; init; } = string.Empty;

    public DigitLetterMapping(int digit, string letters, string examples, string memoryAid)
    {
        Digit = digit;
        Letters = letters;
        Examples = examples;
        MemoryAid = memoryAid;
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
            new(0, "S, soft C, Z", "Sue, Ace, Zoe", "Sounds like 'zero'"),
            new(1, "T, D, TH (both in thing and this)", "Tea, Doe, Thigh, They", "One downstroke"),
            new(2, "N", "No", "Two downstrokes"),
            new(3, "M", "Moo", "Three downstrokes"),
            new(4, "R", "Ray", "Last letter of 'four'"),
            new(5, "L", "Lee", "Roman numeral L = 50"),
            new(6, "CH (in cheese and chef), J, soft G, SH", "Chew, Cha, Jaw, Joe, She", "J looks like reversed 6"),
            new(7, "K, hard C, Q, hard G, CH (as in loch)", "Key, Co, Q, Go, Loch", "K has two 7s"),
            new(8, "F, PH, V", "Fee, Pho, Vee", "F looks like 8"),
            new(9, "P, B", "Pee, Bee", "P looks like reversed 9")
        };
    }

    private static List<DigitLetterMapping> CreateItalianMappings()
    {
        return new List<DigitLetterMapping>
        {
            new(0, "S, SC, Z", "sì, asso, sci, ascia, zio, zoo", "Suona come 'zero'"),
            new(1, "T, D", "tè, dea", "Un tratto verso il basso"),
            new(2, "N, GN", "neo, anno, gnu", "Due tratti verso il basso"),
            new(3, "M", "amo, mio", "Tre tratti verso il basso"),
            new(4, "R", "re, oro", "Ultima lettera di 'quattro'"),
            new(5, "L, GL", "ali, aglio", "Numero romano L = 50"),
            new(6, "C, G dolci", "ciao, ace, oggi", "C sembra un 6 al contrario"),
            new(7, "C, G dure, K", "occhio, eco, qui, ago", "K ha due 7"),
            new(8, "F, V", "ufo, uva, via", "F sembra un 8"),
            new(9, "P, B", "boa, ape", "P sembra un 9 al contrario")
        };
    }
}