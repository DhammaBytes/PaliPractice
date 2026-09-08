using System.Globalization;
using PaliPractice.Localization;

namespace PaliPractice.Tests.Localization;

[TestFixture]
public class AppTextFormatterTests
{
    static readonly CultureInfo RussianCulture = new("ru-RU");

    [TestCase(0, "0 дней")]
    [TestCase(1, "1 день")]
    [TestCase(2, "2 дня")]
    [TestCase(3, "3 дня")]
    [TestCase(4, "4 дня")]
    [TestCase(5, "5 дней")]
    [TestCase(11, "11 дней")]
    [TestCase(12, "12 дней")]
    [TestCase(13, "13 дней")]
    [TestCase(14, "14 дней")]
    [TestCase(20, "20 дней")]
    [TestCase(21, "21 день")]
    [TestCase(22, "22 дня")]
    [TestCase(23, "23 дня")]
    [TestCase(24, "24 дня")]
    [TestCase(25, "25 дней")]
    [TestCase(101, "101 день")]
    [TestCase(102, "102 дня")]
    [TestCase(103, "103 дня")]
    [TestCase(104, "104 дня")]
    [TestCase(105, "105 дней")]
    [TestCase(106, "106 дней")]
    [TestCase(107, "107 дней")]
    [TestCase(108, "108 дней")]
    [TestCase(109, "109 дней")]
    [TestCase(110, "110 дней")]
    [TestCase(111, "111 дней")]
    [TestCase(112, "112 дней")]
    [TestCase(113, "113 дней")]
    [TestCase(114, "114 дней")]
    public void SelectPluralForm_UsesRussianRules(int count, string expected)
    {
        var result = AppTextFormatter.SelectPluralForm(
            count,
            $"{count} день",
            $"{count} дня",
            $"{count} дней",
            RussianCulture);

        AppTextFormatter.SelectPluralForm(count, $"{count} день", $"{count} дня", $"{count} дней",
            CultureInfo.GetCultureInfo("ru-BY")).Should().Be(expected);

        result.Should().Be(expected);
    }
    [TestCase("en-US", 1, "one")]
    [TestCase("en-GB", 0, "many")]
    [TestCase("en-US", 21, "many")]
    public void SelectPluralForm_UsesEnglishRules(string locale, int count, string expected)
    {
        AppTextFormatter.SelectPluralForm(count, "one", "few", "many", CultureInfo.GetCultureInfo(locale))
            .Should().Be(expected);
    }

    [TestCase("ru-RU")]
    [TestCase("ru-BY")]
    public void HistoryDateUsesRussianMonthWithIndependentDeviceRegion(string locale)
    {
        var originalUi = CultureInfo.CurrentUICulture;
        var originalRegion = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var date = new DateTime(2026, 5, 2);
            AppTextFormatter.FormatHistoryHeader(date, date.AddDays(2), date.AddDays(1))
                .Should().Be("2 мая");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalUi;
            CultureInfo.CurrentCulture = originalRegion;
        }
    }
}
