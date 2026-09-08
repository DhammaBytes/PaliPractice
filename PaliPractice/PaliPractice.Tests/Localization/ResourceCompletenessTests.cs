using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace PaliPractice.Tests.Localization;

[TestFixture]
public class ResourceCompletenessTests
{
    // Proper names, language autonyms, and shared punctuation are intentional.
    static readonly string[] SharedRussianValues =
    [
        "Common.Ok", "About.AppNameFormat",
        "Grammar.Table.LikeSuffix"
    ];

    [TestCase("en")]
    [TestCase("ru")]
    public void ResourcesHaveUniqueKeysAndNonemptyValues(string languageCode)
    {
        var entries = LoadEntries(languageCode);
        entries.Select(entry => entry.Attribute("name")?.Value).Should().OnlyHaveUniqueItems();
        foreach (var entry in entries)
        {
            ((string?)entry.Attribute("name")).Should().NotBeNullOrWhiteSpace();
            ((string?)entry.Element("value")).Should().NotBeNullOrWhiteSpace();
        }
    }

    [TestCase("ru")]
    public void TranslationsPreserveKeysPlaceholdersAndMarkdown(string languageCode)
    {
        var english = LoadValues("en");
        var translated = LoadValues(languageCode);
        translated.Keys.Should().BeEquivalentTo(english.Keys);

        foreach (var (key, source) in english)
        {
            var value = translated[key];
            using var scope = new FluentAssertions.Execution.AssertionScope($"{languageCode}/{key}");
            FormatArguments(value).Should().BeEquivalentTo(FormatArguments(source));
            var sourceMarkdown = Markdown.Parse(source);
            var translatedMarkdown = Markdown.Parse(value);
            translatedMarkdown.Descendants<LinkInline>().Select(link => link.Url)
                .Should().BeEquivalentTo(sourceMarkdown.Descendants<LinkInline>().Select(link => link.Url));
            translatedMarkdown.Descendants<CodeInline>().Select(code => code.Content)
                .Should().BeEquivalentTo(sourceMarkdown.Descendants<CodeInline>().Select(code => code.Content));
            translatedMarkdown.Descendants<EmphasisInline>().Select(emphasis => emphasis.DelimiterCount)
                .Should().BeEquivalentTo(sourceMarkdown.Descendants<EmphasisInline>().Select(emphasis => emphasis.DelimiterCount));
        }
    }

    [Test]
    public void RussianOnlySharesReviewedEnglishValues()
    {
        var english = LoadValues("en");
        var russian = LoadValues("ru");
        english.Where(entry => russian[entry.Key] == entry.Value).Select(entry => entry.Key)
            .Should().BeEquivalentTo(SharedRussianValues);
    }

    static IReadOnlyList<(int Index, string? Format)> FormatArguments(string value)
    {
        var parsed = CompositeFormat.Parse(value);
        var recorder = new ArgumentRecorder();
        var arguments = Enumerable.Range(0, parsed.MinimumArgumentCount).Cast<object>().ToArray();
        _ = string.Format(recorder, parsed, arguments);
        return recorder.Arguments;
    }

    sealed class ArgumentRecorder : IFormatProvider, ICustomFormatter
    {
        public List<(int Index, string? Format)> Arguments { get; } = [];
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            Arguments.Add(((int)arg!, format));
            return string.Empty;
        }
    }

    static Dictionary<string, string> LoadValues(string languageCode)
        => LoadEntries(languageCode).ToDictionary(
            entry => entry.Attribute("name")!.Value,
            entry => entry.Element("value")!.Value,
            StringComparer.Ordinal);

    static XElement[] LoadEntries(string languageCode)
    {
        var path = System.IO.Path.Combine(TestPaths.RepositoryRoot, "PaliPractice", "PaliPractice",
            "Strings", languageCode, "Resources.resw");
        File.Exists(path).Should().BeTrue($"resource file should exist: {path}");
        return XDocument.Load(path).Root!.Elements("data").ToArray();
    }
}
