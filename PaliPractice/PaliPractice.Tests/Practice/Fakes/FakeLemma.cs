using PaliPractice.Models;
using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;

namespace PaliPractice.Tests.Practice.Fakes;

/// <summary>
/// Fake implementation of ILemma for testing.
/// Uses real Noun/Verb objects to ensure compatibility with PracticeQueueBuilder
/// which casts to concrete types.
/// </summary>
public class FakeLemma : ILemma
{
    public int LemmaId { get; set; }
    public int EbtCount { get; set; }
    public string BaseForm { get; set; } = "";
    public IReadOnlyList<IWord> Words { get; set; } = [];
    public IWord Primary => Words[0];
    public IReadOnlyList<IWord> ExcludedWords { get; } = [];
    public bool HasDetails { get; private set; } = true;

    public void LoadDetails(IReadOnlyList<IWordDetails> details)
    {
        HasDetails = true;
    }

    /// <summary>
    /// Creates a fake noun lemma using a real Noun object.
    /// This is required because PracticeQueueBuilder casts to concrete Noun type.
    /// </summary>
    public static FakeLemma CreateNoun(
        int lemmaId,
        string baseForm,
        Gender gender,
        NounPattern pattern,
        int ebtCount = 100)
    {
        // Use the real Noun class to avoid cast exceptions in PracticeQueueBuilder
        var noun = new Noun
        {
            Id = lemmaId,
            LemmaId = lemmaId,
            Lemma = baseForm,
            Stem = baseForm,
            Gender = gender,
            EbtCount = ebtCount,
            RawPattern = pattern.ToDbString()
        };

        return new FakeLemma
        {
            LemmaId = lemmaId,
            BaseForm = baseForm,
            EbtCount = ebtCount,
            Words = [noun]
        };
    }

    /// <summary>
    /// Creates a fake verb lemma using a real Verb object.
    /// This is required because PracticeQueueBuilder casts to concrete Verb type.
    /// </summary>
    public static FakeLemma CreateVerb(
        int lemmaId,
        string baseForm,
        VerbPattern pattern,
        int ebtCount = 100)
    {
        // Use the real Verb class to avoid cast exceptions in PracticeQueueBuilder
        var verb = new Verb
        {
            Id = lemmaId,
            LemmaId = lemmaId,
            Lemma = baseForm,
            Stem = baseForm,
            EbtCount = ebtCount,
            RawPattern = pattern.ToDbString()
        };

        return new FakeLemma
        {
            LemmaId = lemmaId,
            BaseForm = baseForm,
            EbtCount = ebtCount,
            Words = [verb]
        };
    }
}
