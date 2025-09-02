using PaliPractice.Models;

namespace PaliPractice.Presentation.Helpers;

public static class OptionPresentation
{
    public static string? GetGlyph<T>(T value) where T : struct, Enum => value switch
    {
        Number.Singular => "\uE77B",
        Number.Plural => "\uE716",
        
        Person.First => "\uE77B",
        Person.Second => "\uE748",
        Person.Third => "\uE716",
        
        Voice.Normal => "\uE768",
        Voice.Reflexive => "\uE74C",
        
        Gender.Masculine => "\uE71A",
        Gender.Neuter => "\uE734",
        Gender.Feminine => "\uE716",
        
        _ => null
    };

    public static Color? GetChipColor<T>(T value) where T : struct, Enum => value switch
    {
        Number.Singular => (Color?)Color.FromArgb(255, 230, 230, 255),
        Number.Plural => (Color?)Color.FromArgb(255, 230, 230, 255),
        
        Person.First => (Color?)Color.FromArgb(255, 230, 255, 230),
        Person.Second => (Color?)Color.FromArgb(255, 230, 255, 230),
        Person.Third => (Color?)Color.FromArgb(255, 230, 255, 230),
        
        Voice.Normal => (Color?)Color.FromArgb(255, 255, 230, 230),
        Voice.Reflexive => (Color?)Color.FromArgb(255, 255, 230, 230),
        
        Tense.Present => (Color?)Color.FromArgb(255, 240, 240, 255),
        Tense.Imperative => (Color?)Color.FromArgb(255, 240, 240, 255),
        Tense.Aorist => (Color?)Color.FromArgb(255, 240, 240, 255),
        Tense.Optative => (Color?)Color.FromArgb(255, 240, 240, 255),
        Tense.Future => (Color?)Color.FromArgb(255, 240, 240, 255),
        
        Gender.Masculine => (Color?)Color.FromArgb(255, 220, 255, 220),
        Gender.Neuter => (Color?)Color.FromArgb(255, 220, 255, 220),
        Gender.Feminine => (Color?)Color.FromArgb(255, 220, 255, 220),
        
        NounCase.Nominative => (Color?)Color.FromArgb(255, 255, 243, 224),
        NounCase.Accusative => (Color?)Color.FromArgb(255, 255, 243, 224),
        NounCase.Instrumental => (Color?)Color.FromArgb(255, 255, 243, 224),
        NounCase.Dative => (Color?)Color.FromArgb(255, 255, 243, 224),
        NounCase.Ablative => (Color?)Color.FromArgb(255, 255, 243, 224),
        NounCase.Genitive => (Color?)Color.FromArgb(255, 255, 243, 224),
        NounCase.Locative => (Color?)Color.FromArgb(255, 255, 243, 224),
        NounCase.Vocative => (Color?)Color.FromArgb(255, 255, 243, 224),
        
        _ => null
    };
}