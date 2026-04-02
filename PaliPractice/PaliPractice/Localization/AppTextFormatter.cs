using System.Globalization;
using PaliPractice.Services.UserData;

namespace PaliPractice.Localization;

public static class AppTextFormatter
{
    public static string FormatAppNameWithVersion(string version)
        => AppText.Format("About.AppNameFormat", version);

    public static string FormatPageOf(int current, int total)
        => AppText.Format("Common.PageOfFormat", current, total);

    public static string FormatAvailableCount(int count)
        => AppText.Format("Common.AvailableCount", count);

    public static string FormatDayCount(int count, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;

        var one = string.Format(culture, AppText.Get("Common.DayCount.One"), count);
        var few = string.Format(culture, AppText.Get("Common.DayCount.Few"), count);
        var many = string.Format(culture, AppText.Get("Common.DayCount.Many"), count);

        return SelectPluralForm(count, one, few, many, culture);
    }

    public static string SelectPluralForm(
        int count,
        string one,
        string few,
        string many,
        CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;

        if (string.Equals(
                culture.TwoLetterISOLanguageName,
                TranslationLanguageResolver.RussianLanguageCode,
                StringComparison.OrdinalIgnoreCase))
        {
            return SelectRussianPluralForm(count, one, few, many);
        }

        return count == 1 ? one : many;
    }

    public static string SelectRussianPluralForm(int count, string one, string few, string many)
    {
        var normalized = Math.Abs(count) % 100;
        var lastDigit = normalized % 10;

        if (normalized is >= 11 and <= 14)
            return many;

        return lastDigit switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many
        };
    }

    public static string FormatHistoryHeader(DateTime date, DateTime today, DateTime yesterday)
    {
        if (date == today)
            return AppText.Get("History.Header.Today");
        if (date == yesterday)
            return AppText.Get("History.Header.Yesterday");

        return date.ToString("d MMM", CultureInfo.CurrentUICulture);
    }

    public static string FormatCalendarTooltip(DateTime date, int declensionsCount, int conjugationsCount)
    {
        var dateText = date.ToString("ddd, d MMM", CultureInfo.CurrentUICulture);
        return AppText.Format("Statistics.CalendarTooltipFormat", dateText, declensionsCount, conjugationsCount);
    }
}
