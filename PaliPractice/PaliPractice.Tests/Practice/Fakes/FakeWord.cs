using PaliPractice.Models.Words;

namespace PaliPractice.Tests.Practice.Fakes;

/// <summary>
/// Fake implementation of IWord for testing.
/// Supports both nouns and verbs via the same base implementation.
/// </summary>
public class FakeWord : IWord
{
    public int Id { get; set; }
    public int EbtCount { get; set; }
    public int LemmaId { get; set; }
    public string Lemma { get; set; } = "";
    public string? Stem { get; set; }
    public string RawPattern { get; set; } = "";
    public bool Irregular { get; set; }
    public IWordDetails? Details { get; set; }
}

/// <summary>
/// Fake noun with Gender and Pattern support.
/// </summary>
public class FakeNoun : FakeWord
{
    public Gender Gender { get; set; }
    public NounPattern Pattern { get; set; }

    public FakeNoun()
    {
        RawPattern = Pattern.ToString();
    }
}

/// <summary>
/// Fake verb with Pattern support.
/// </summary>
public class FakeVerb : FakeWord
{
    public VerbPattern Pattern { get; set; }

    public FakeVerb()
    {
        RawPattern = Pattern.ToString();
    }
}
