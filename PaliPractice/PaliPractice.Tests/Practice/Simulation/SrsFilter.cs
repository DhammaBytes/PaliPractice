using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;

namespace PaliPractice.Tests.Practice.Simulation;

internal sealed record SrsFilter(string Name, int MinRank, int MaxRank, int[] Categories,
    int[] Numbers, int[] Persons, int[] Voices, NounPattern[] Masculine,
    NounPattern[] Feminine, NounPattern[] Neuter, VerbPattern[] Verbs, string[]? RawPatterns = null)
{
    public static SrsFilter Default(PracticeType type) => new("default", 1, 100,
        type == PracticeType.Declension ? [1, 2] : [1], [1, 2], [1, 2, 3], [1],
        SettingsKeys.NounsDefaultMascPatterns, SettingsKeys.NounsDefaultFemPatterns,
        SettingsKeys.NounsDefaultNeutPatterns, SettingsKeys.VerbsDefaultPatterns);

    public static SrsFilter Broad(PracticeType type) => Default(type) with
    {
        Name = "broad", MaxRank = type == PracticeType.Declension ? 1500 : 750,
        Categories = type == PracticeType.Declension ? [1, 2, 3, 4, 5, 6, 7, 8] : [1, 2, 3, 4],
        Voices = [1, 2]
    };

    public static SrsFilter Rare(PracticeType type) => type == PracticeType.Declension
        ? Broad(type) with { Name = "feminine-i-oblique-plural", Categories = [3, 4, 5, 6, 7], Numbers = [2],
            Masculine = [], Neuter = [], Feminine = [NounPattern.ĪFem],
            RawPatterns = ["ī fem", "vī fem", "nadī fem", "pokkharaṇī fem"] }
        : Broad(type) with { Name = "oti-optative-future", Categories = [3, 4], Persons = [1, 2],
            Verbs = [VerbPattern.Oti], RawPatterns = ["oti pr", "brūti pr", "karoti pr"] };

    // Rank controls require min < max. Combine a two-rank window with a pattern
    // filter to produce one eligible lemma in the pinned dictionary.
    public static SrsFilter OneLemma(PracticeType type) => type == PracticeType.Declension
        ? Broad(type) with { Name = "one-lemma", MinRank = 2, MaxRank = 3,
            Masculine = [], Neuter = [], Feminine = [NounPattern.ĀFem], RawPatterns = ["ā fem", "parisā fem"] }
        : Broad(type) with { Name = "one-lemma", MaxRank = 2,
            Verbs = [VerbPattern.Eti], RawPatterns = ["eti pr", "eti pr 2"] };

    public void Apply(UserDataRepository data, PracticeType type)
    {
        if (type == PracticeType.Declension)
        {
            data.SetSetting(SettingsKeys.NounsLemmaMin, MinRank);
            data.SetSetting(SettingsKeys.NounsLemmaMax, MaxRank);
            data.SetSetting(SettingsKeys.NounsCases, string.Join(',', Categories));
            data.SetSetting(SettingsKeys.NounsNumbers, string.Join(',', Numbers));
            data.SetSetting(SettingsKeys.NounsMascPatterns, SettingsHelpers.ToCsv(Masculine));
            data.SetSetting(SettingsKeys.NounsFemPatterns, SettingsHelpers.ToCsv(Feminine));
            data.SetSetting(SettingsKeys.NounsNeutPatterns, SettingsHelpers.ToCsv(Neuter));
        }
        else
        {
            data.SetSetting(SettingsKeys.VerbsLemmaMin, MinRank);
            data.SetSetting(SettingsKeys.VerbsLemmaMax, MaxRank);
            data.SetSetting(SettingsKeys.VerbsTenses, string.Join(',', Categories));
            data.SetSetting(SettingsKeys.VerbsNumbers, string.Join(',', Numbers));
            data.SetSetting(SettingsKeys.VerbsPersons, string.Join(',', Persons));
            data.SetSetting(SettingsKeys.VerbsVoices, string.Join(',', Voices));
            data.SetSetting(SettingsKeys.VerbsPatterns, SettingsHelpers.ToCsv(Verbs));
        }
    }
}
