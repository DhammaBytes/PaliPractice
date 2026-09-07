using PaliPractice.Models.Inflection;

namespace PaliPractice.Services.UserData.Entities;

/// <summary>Immutable display evidence captured before the practice card advances.</summary>
public sealed record PracticeSnapshot(string FormText, string LemmaText, string GrammarText)
{
    public static PracticeSnapshot Capture(long formId, PracticeType type, string form, string lemma)
    {
        if (string.IsNullOrWhiteSpace(form) || string.IsNullOrWhiteSpace(lemma))
            throw new ArgumentException("A practice snapshot requires form and lemma text");
        return new PracticeSnapshot(form, lemma, Grammar(formId, type));
    }

    static string Grammar(long formId, PracticeType type)
    {
        if (type == PracticeType.Declension)
        {
            var parsed = Declension.ParseId(formId);
            return $"{parsed.Case}/{parsed.Gender}/{parsed.Number}";
        }
        if (type == PracticeType.Conjugation)
        {
            var parsed = Conjugation.ParseId(formId);
            return $"{parsed.Tense}/{parsed.Person}/{parsed.Number}/{parsed.Voice}";
        }
        throw new ArgumentOutOfRangeException(nameof(type));
    }
}
