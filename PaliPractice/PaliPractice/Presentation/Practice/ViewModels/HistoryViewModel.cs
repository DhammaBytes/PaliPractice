using System.Collections.ObjectModel;
using PaliPractice.Services.Grammar;
using PaliPractice.Services.UserData.Entities;

namespace PaliPractice.Presentation.Practice.ViewModels;

/// <summary>
/// Navigation data wrapper for PracticeType (needed because DataViewMap requires reference types).
/// </summary>
public record HistoryNavigationData(PracticeType PracticeType);

/// <summary>
/// A section of history records grouped by date.
/// </summary>
public class HistorySection
{
    public required string Header { get; init; }
    public required List<IPracticeHistory> Records { get; init; }
}

[Bindable]
public class HistoryViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public HistoryViewModel(INavigator navigator, IDatabaseService db, IInflectionService inflection, HistoryNavigationData data)
    {
        _navigator = navigator;
        CurrentPracticeType = data.PracticeType;

        // Load history from database and resolve form text from FormId
        var history = db.UserData.GetRecentHistory(data.PracticeType, limit: 1000);
        foreach (var record in history)
        {
            record.FormText = inflection.ResolveFormText(record.FormId, data.PracticeType) ?? "?";
        }

        // Group records by date
        Sections = GroupByDate(history);
    }

    PracticeType CurrentPracticeType { get; }

    public string Title => CurrentPracticeType == PracticeType.Declension
        ? "Declension History"
        : "Conjugation History";

    public List<HistorySection> Sections { get; }

    public bool HasHistory => Sections.Count > 0;

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    /// <summary>
    /// Groups history records by logical date (Today, Yesterday, or specific date).
    /// </summary>
    static List<HistorySection> GroupByDate(IEnumerable<IPracticeHistory> records)
    {
        var now = DateTime.Now;
        // Use logical day (5am boundary) like DailyProgress
        var todayLogical = now.Hour < 5 ? now.Date.AddDays(-1) : now.Date;
        var yesterdayLogical = todayLogical.AddDays(-1);

        var grouped = records
            .GroupBy(r =>
            {
                var localTime = r.PracticedUtc.ToLocalTime();
                var logicalDate = localTime.Hour < 5 ? localTime.Date.AddDays(-1) : localTime.Date;
                return logicalDate;
            })
            .OrderByDescending(g => g.Key)
            .Select(g => new HistorySection
            {
                Header = FormatDateHeader(g.Key, todayLogical, yesterdayLogical),
                Records = g.ToList()
            })
            .ToList();

        return grouped;
    }

    /// <summary>
    /// Formats a date as "Today", "Yesterday", or "5 Jan" style.
    /// </summary>
    static string FormatDateHeader(DateTime date, DateTime today, DateTime yesterday)
    {
        if (date == today)
            return "Today";
        if (date == yesterday)
            return "Yesterday";

        // Format as "5 Jan" (day + abbreviated month)
        return date.ToString("d MMM");
    }
}
