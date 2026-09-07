using PaliPractice.Services.Database.Repositories;

namespace PaliPractice.Services.UserData;

/// <summary>
/// Reads required verb filters and repairs selections that contain only the active citation form.
/// Settings and practice use the same persisted recovery rules.
/// </summary>
internal static class VerbFilterSettings
{
    public static (HashSet<VerbPattern> Patterns, List<Tense> Tenses, List<Person> Persons,
        List<Number> Numbers, List<Voice> Voices) Load(IUserDataRepository userData)
    {
        var patterns = userData.GetEnumSetOrResetDefault(SettingsKeys.VerbsPatterns, SettingsKeys.VerbsDefaultPatterns);
        var tenses = userData.GetEnumListOrResetDefault(SettingsKeys.VerbsTenses, SettingsKeys.VerbsDefaultTenses);
        var persons = userData.GetEnumListOrResetDefault(SettingsKeys.VerbsPersons, SettingsKeys.VerbsDefaultPersons);
        var numbers = userData.GetEnumListOrResetDefault(SettingsKeys.VerbsNumbers, SettingsKeys.DefaultNumbers);
        var voices = userData.GetEnumListOrResetDefault(SettingsKeys.VerbsVoices, SettingsKeys.VerbsDefaultVoices);

        // Each required selection is now nonempty. Match the settings UI's correction:
        // keep the tense, number, and voice, and restore the default persons.
        if (tenses.All(tense => tense == Tense.Present) &&
            persons.All(person => person == Person.Third) &&
            numbers.All(number => number == Number.Singular) &&
            voices.All(voice => voice == Voice.Active))
        {
            persons = SettingsKeys.VerbsDefaultPersons.Order().ToList();
            userData.SetSetting(SettingsKeys.VerbsPersons, SettingsHelpers.ToCsv(persons));
        }

        return (patterns, tenses, persons, numbers, voices);
    }
}
