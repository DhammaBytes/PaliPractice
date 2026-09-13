using System.Text;

namespace PaliPractice.Tests.DataIntegrity;

[TestFixture]
public class UnicodeCharacterTests
{
    [Test]
    public void PaliDiacriticals_ArePrecomposedCharacters()
    {
        // Verify that our reference diacriticals are themselves NFC
        foreach (var c in "āīūṁṃñḍḷṭṇ")
        {
            var str = c.ToString();
            var nfc = str.Normalize(NormalizationForm.FormC);

            nfc.Length.Should().Be(1,
                $"'{c}' (U+{(int)c:X4}) should be a single precomposed character, not decomposed");
        }
    }
}
