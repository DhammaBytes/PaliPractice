using System.Xml.Linq;

namespace PaliPractice.Tests.Localization;

[TestFixture]
public class ResourceCompletenessTests
{
    [Test]
    public void EnglishAndRussianResourceKeysMatch()
    {
        var englishKeys = LoadKeys("en");
        var russianKeys = LoadKeys("ru");

        englishKeys.Should().BeEquivalentTo(russianKeys);
    }

    static HashSet<string> LoadKeys(string languageCode)
    {
        var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "PaliPractice", "PaliPractice", "Strings", languageCode, "Resources.resw"));

        File.Exists(path).Should().BeTrue($"resource file should exist: {path}");

        var document = XDocument.Load(path);
        return document.Root!
            .Elements("data")
            .Select(element => element.Attribute("name")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
    }
}
