namespace PaliPractice.Localization;

public static class GrammarText
{
    public static string GetCase(Case value) => value switch
    {
        Case.Nominative => AppText.Get("Grammar.Case.Nominative.Full"),
        Case.Accusative => AppText.Get("Grammar.Case.Accusative.Full"),
        Case.Instrumental => AppText.Get("Grammar.Case.Instrumental.Full"),
        Case.Dative => AppText.Get("Grammar.Case.Dative.Full"),
        Case.Ablative => AppText.Get("Grammar.Case.Ablative.Full"),
        Case.Genitive => AppText.Get("Grammar.Case.Genitive.Full"),
        Case.Locative => AppText.Get("Grammar.Case.Locative.Full"),
        Case.Vocative => AppText.Get("Grammar.Case.Vocative.Full"),
        _ => value.ToString()
    };

    public static string GetCaseShort(Case value) => value switch
    {
        Case.Nominative => AppText.Get("Grammar.Case.Nominative.Short"),
        Case.Accusative => AppText.Get("Grammar.Case.Accusative.Short"),
        Case.Instrumental => AppText.Get("Grammar.Case.Instrumental.Short"),
        Case.Dative => AppText.Get("Grammar.Case.Dative.Short"),
        Case.Ablative => AppText.Get("Grammar.Case.Ablative.Short"),
        Case.Genitive => AppText.Get("Grammar.Case.Genitive.Short"),
        Case.Locative => AppText.Get("Grammar.Case.Locative.Short"),
        Case.Vocative => AppText.Get("Grammar.Case.Vocative.Short"),
        _ => value.ToString()
    };

    public static string GetCaseTable(Case value) => value switch
    {
        Case.Nominative => AppText.Get("Grammar.Case.Nominative.Table"),
        Case.Accusative => AppText.Get("Grammar.Case.Accusative.Table"),
        Case.Instrumental => AppText.Get("Grammar.Case.Instrumental.Table"),
        Case.Dative => AppText.Get("Grammar.Case.Dative.Table"),
        Case.Ablative => AppText.Get("Grammar.Case.Ablative.Table"),
        Case.Genitive => AppText.Get("Grammar.Case.Genitive.Table"),
        Case.Locative => AppText.Get("Grammar.Case.Locative.Table"),
        Case.Vocative => AppText.Get("Grammar.Case.Vocative.Table"),
        _ => value.ToString()
    };

    public static string GetCasePracticeHint(Case value) => value switch
    {
        Case.Nominative => AppText.Get("Grammar.Case.Nominative.PracticeHint"),
        Case.Accusative => AppText.Get("Grammar.Case.Accusative.PracticeHint"),
        Case.Instrumental => AppText.Get("Grammar.Case.Instrumental.PracticeHint"),
        Case.Dative => AppText.Get("Grammar.Case.Dative.PracticeHint"),
        Case.Ablative => AppText.Get("Grammar.Case.Ablative.PracticeHint"),
        Case.Genitive => AppText.Get("Grammar.Case.Genitive.PracticeHint"),
        Case.Locative => AppText.Get("Grammar.Case.Locative.PracticeHint"),
        Case.Vocative => AppText.Get("Grammar.Case.Vocative.PracticeHint"),
        _ => string.Empty
    };

    public static string GetCaseSettingsHint(Case value) => value switch
    {
        Case.Nominative => AppText.Get("Grammar.Case.Nominative.SettingsHint"),
        Case.Accusative => AppText.Get("Grammar.Case.Accusative.SettingsHint"),
        Case.Instrumental => AppText.Get("Grammar.Case.Instrumental.SettingsHint"),
        Case.Dative => AppText.Get("Grammar.Case.Dative.SettingsHint"),
        Case.Ablative => AppText.Get("Grammar.Case.Ablative.SettingsHint"),
        Case.Genitive => AppText.Get("Grammar.Case.Genitive.SettingsHint"),
        Case.Locative => AppText.Get("Grammar.Case.Locative.SettingsHint"),
        Case.Vocative => AppText.Get("Grammar.Case.Vocative.SettingsHint"),
        _ => string.Empty
    };

    public static string GetTense(Tense value) => value switch
    {
        Tense.Present => AppText.Get("Grammar.Tense.Present.Full"),
        Tense.Imperative => AppText.Get("Grammar.Tense.Imperative.Full"),
        Tense.Optative => AppText.Get("Grammar.Tense.Optative.Full"),
        Tense.Future => AppText.Get("Grammar.Tense.Future.Full"),
        _ => value.ToString()
    };

    public static string GetTenseShort(Tense value) => value switch
    {
        Tense.Present => AppText.Get("Grammar.Tense.Present.Short"),
        Tense.Imperative => AppText.Get("Grammar.Tense.Imperative.Short"),
        Tense.Optative => AppText.Get("Grammar.Tense.Optative.Short"),
        Tense.Future => AppText.Get("Grammar.Tense.Future.Short"),
        _ => value.ToString()
    };

    public static string GetTenseTable(Tense value) => value switch
    {
        Tense.Present => AppText.Get("Grammar.Tense.Present.Table"),
        Tense.Imperative => AppText.Get("Grammar.Tense.Imperative.Table"),
        Tense.Optative => AppText.Get("Grammar.Tense.Optative.Table"),
        Tense.Future => AppText.Get("Grammar.Tense.Future.Table"),
        _ => value.ToString()
    };

    public static string GetTenseSettingsHint(Tense value) => value switch
    {
        Tense.Present => AppText.Get("Grammar.Tense.Present.SettingsHint"),
        Tense.Imperative => AppText.Get("Grammar.Tense.Imperative.SettingsHint"),
        Tense.Optative => AppText.Get("Grammar.Tense.Optative.SettingsHint"),
        Tense.Future => AppText.Get("Grammar.Tense.Future.SettingsHint"),
        _ => string.Empty
    };

    public static string GetGender(Gender value) => value switch
    {
        Gender.Masculine => AppText.Get("Grammar.Gender.Masculine.Full"),
        Gender.Feminine => AppText.Get("Grammar.Gender.Feminine.Full"),
        Gender.Neuter => AppText.Get("Grammar.Gender.Neuter.Full"),
        _ => value.ToString()
    };

    public static string GetGenderShort(Gender value) => value switch
    {
        Gender.Masculine => AppText.Get("Grammar.Gender.Masculine.Short"),
        Gender.Feminine => AppText.Get("Grammar.Gender.Feminine.Short"),
        Gender.Neuter => AppText.Get("Grammar.Gender.Neuter.Short"),
        _ => value.ToString()
    };

    public static string GetGenderTable(Gender value) => value switch
    {
        Gender.Masculine => AppText.Get("Grammar.Gender.Masculine.Table"),
        Gender.Feminine => AppText.Get("Grammar.Gender.Feminine.Table"),
        Gender.Neuter => AppText.Get("Grammar.Gender.Neuter.Table"),
        _ => value.ToString()
    };

    public static string GetNumber(Number value) => value switch
    {
        Number.Singular => AppText.Get("Grammar.Number.Singular.Full"),
        Number.Plural => AppText.Get("Grammar.Number.Plural.Full"),
        _ => value.ToString()
    };

    public static string GetNumberShort(Number value) => value switch
    {
        Number.Singular => AppText.Get("Grammar.Number.Singular.Short"),
        Number.Plural => AppText.Get("Grammar.Number.Plural.Short"),
        _ => value.ToString()
    };

    public static string GetNumberTable(Number value) => value switch
    {
        Number.Singular => AppText.Get("Grammar.Number.Singular.Table"),
        Number.Plural => AppText.Get("Grammar.Number.Plural.Table"),
        _ => value.ToString()
    };

    public static string GetPerson(Person value) => value switch
    {
        Person.First => AppText.Get("Grammar.Person.First.Full"),
        Person.Second => AppText.Get("Grammar.Person.Second.Full"),
        Person.Third => AppText.Get("Grammar.Person.Third.Full"),
        _ => value.ToString()
    };

    public static string GetPersonShort(Person value) => value switch
    {
        Person.First => AppText.Get("Grammar.Person.First.Short"),
        Person.Second => AppText.Get("Grammar.Person.Second.Short"),
        Person.Third => AppText.Get("Grammar.Person.Third.Short"),
        _ => value.ToString()
    };

    public static string GetVoice(Voice value) => value switch
    {
        Voice.Active => AppText.Get("Grammar.Voice.Active.Full"),
        Voice.Reflexive => AppText.Get("Grammar.Voice.Reflexive.Full"),
        _ => value.ToString()
    };

    public static string GetVoiceShort(Voice value) => value switch
    {
        Voice.Active => AppText.Get("Grammar.Voice.Active.Short"),
        Voice.Reflexive => AppText.Get("Grammar.Voice.Reflexive.Short"),
        _ => value.ToString()
    };

    public static string GetVoiceTable(Voice value) => value switch
    {
        Voice.Reflexive => AppText.Get("Grammar.Voice.Reflexive.Table"),
        _ => GetVoiceShort(value)
    };

    public static string GetPatternTypeName(bool isNoun, bool isIrregular, bool isVariantPattern)
    {
        if (isNoun)
        {
            if (isIrregular) return AppText.Get("Grammar.Type.IrregularDeclension");
            if (isVariantPattern) return AppText.Get("Grammar.Type.NonStandardDeclension");
            return AppText.Get("Grammar.Type.Declension");
        }

        if (isIrregular) return AppText.Get("Grammar.Type.IrregularConjugation");
        return AppText.Get("Grammar.Type.Conjugation");
    }

    public static IReadOnlyList<string> GetAllCaseLabels()
        => Enum.GetValues<Case>()
            .Where(value => value != Case.None)
            .Select(GetCase)
            .ToArray();

    public static IReadOnlyList<string> GetAllGenderLabels()
        => Enum.GetValues<Gender>()
            .Where(value => value != Gender.None)
            .Select(GetGender)
            .ToArray();

    public static IReadOnlyList<string> GetAllNumberLabels()
        => Enum.GetValues<Number>()
            .Where(value => value != Number.None)
            .Select(GetNumber)
            .ToArray();

    public static IReadOnlyList<string> GetAllTenseLabels()
        => Enum.GetValues<Tense>()
            .Where(value => value != Tense.None)
            .Select(GetTense)
            .ToArray();

    public static IReadOnlyList<string> GetAllPersonLabels()
        => Enum.GetValues<Person>()
            .Where(value => value != Person.None)
            .Select(GetPerson)
            .ToArray();
}
