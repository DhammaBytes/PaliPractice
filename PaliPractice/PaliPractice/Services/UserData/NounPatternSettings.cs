using PaliPractice.Services.Database.Repositories;

namespace PaliPractice.Services.UserData;

/// <summary>
/// Reads noun pattern filters with the same recovery rules for settings and practice.
/// Individual genders may be empty, but an entirely empty selection recovers to defaults.
/// </summary>
internal static class NounPatternSettings
{
    public static (HashSet<NounPattern> Masculine, HashSet<NounPattern> Neuter, HashSet<NounPattern> Feminine)
        Load(IUserDataRepository userData)
    {
        var patterns = Read(userData, allowEmpty: true);
        return patterns.Masculine.Count == 0 && patterns.Neuter.Count == 0 && patterns.Feminine.Count == 0
            ? Read(userData, allowEmpty: false)
            : patterns;
    }

    static (HashSet<NounPattern> Masculine, HashSet<NounPattern> Neuter, HashSet<NounPattern> Feminine)
        Read(IUserDataRepository userData, bool allowEmpty) =>
        (
            userData.GetEnumSetOrResetDefault(
                SettingsKeys.NounsMascPatterns, SettingsKeys.NounsDefaultMascPatterns, allowEmpty),
            userData.GetEnumSetOrResetDefault(
                SettingsKeys.NounsNeutPatterns, SettingsKeys.NounsDefaultNeutPatterns, allowEmpty),
            userData.GetEnumSetOrResetDefault(
                SettingsKeys.NounsFemPatterns, SettingsKeys.NounsDefaultFemPatterns, allowEmpty)
        );
}
