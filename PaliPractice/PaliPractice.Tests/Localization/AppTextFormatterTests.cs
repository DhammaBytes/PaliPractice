using System.Globalization;
using PaliPractice.Localization;

namespace PaliPractice.Tests.Localization;

[TestFixture]
public class AppTextFormatterTests
{
    static readonly CultureInfo RussianCulture = new("ru-RU");

    [TestCase(1, "1 день")]
    [TestCase(2, "2 дня")]
    [TestCase(5, "5 дней")]
    [TestCase(21, "21 день")]
    [TestCase(22, "22 дня")]
    [TestCase(25, "25 дней")]
    public void SelectPluralForm_UsesRussianRules(int count, string expected)
    {
        var result = AppTextFormatter.SelectPluralForm(
            count,
            $"{count} день",
            $"{count} дня",
            $"{count} дней",
            RussianCulture);

        result.Should().Be(expected);
    }
}
