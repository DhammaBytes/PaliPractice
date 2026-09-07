using PaliPractice.Tests.DataIntegrity.Helpers;

namespace PaliPractice.Tests.DataIntegrity;

[TestFixture]
public class CustomTranslationsLoaderTests
{
    [Test]
    public void Load_AppliesBothSectionsOnlyToMatchingSenseAndIgnoresIncompleteEntries()
    {
        var path = System.IO.Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
                {
                  "primary": {
                    "1": {"lemma_1":"paññā 1", "preferred":"wisdom"},
                    "invalid": {"lemma_1":"paññā 1", "preferred":"wrong"},
                    "2": {"lemma_1":"paññā 2"},
                    "3": {"lemma_1":null, "preferred":"wrong"}
                  },
                  "replace": {
                    "1": {"lemma_1":"paññā 1", "target":"knowledge", "preferred":"understanding"},
                    "4": {"lemma_1":"paññā 4", "target":"knowledge"}
                  }
                }
                """);
            var loader = new CustomTranslationsLoader(path);
            Assert.Multiple(() =>
            {
                Assert.That(loader.Count, Is.EqualTo(2));
                Assert.That(loader.Apply(1, "paññā 1", "knowledge; Wisdom of things"),
                    Is.EqualTo("Wisdom of things; understanding"));
                Assert.That(loader.Apply(1, "paññā 2", "knowledge"), Is.EqualTo("knowledge"));
                Assert.That(loader.HasAdjustment(2), Is.False);
                Assert.That(loader.HasAdjustment(3), Is.False);
                Assert.That(loader.HasAdjustment(4), Is.False);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }
}
