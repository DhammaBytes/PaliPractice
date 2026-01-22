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

#if DEBUG
        if (ScreenshotMode.IsEnabled)
        {
            Sections = CreateScreenshotHistory(data.PracticeType);
            return;
        }
#endif

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

#if DEBUG
    #region Screenshot Mode

    /// <summary>
    /// Mock implementation of IPracticeHistory for screenshot mode.
    /// </summary>
    class MockPracticeHistory : IPracticeHistory
    {
        public int Id { get; init; }
        public long FormId { get; init; }
        public string FormText { get; set; } = "";
        public int OldLevel { get; init; }
        public int NewLevel { get; init; }
        public DateTime PracticedUtc { get; init; }
        public bool IsImproved => NewLevel > OldLevel;
        public int NewLevelPercent => Math.Min(NewLevel, 10) * 10;
    }

    /// <summary>
    /// Creates mock history data for screenshots.
    /// 20 items: 12 today + 8 yesterday, with varied easy/hard progression.
    /// </summary>
    static List<HistorySection> CreateScreenshotHistory(PracticeType type)
    {
        var records = type == PracticeType.Declension
            ? CreateMockNounHistory()
            : CreateMockVerbHistory();

        return GroupByDate(records);
    }

    /// <summary>
    /// Creates 20 mock noun history records with high-EBT forms.
    /// </summary>
    static List<IPracticeHistory> CreateMockNounHistory()
    {
        var now = DateTime.UtcNow;
        var yesterday8pm = now.Date.AddDays(-1).AddHours(20);

        // Forms for today (12 items) and yesterday (8 items)
        var todayForms = new[]
        {
            "bhikkhuno", "bhikkhave", "dhammato", "bhagavā",
            "bhante", "āyasmato", "cittena", "samaye", "brāhmaṇassa"
        };
        var yesterdayForms = new[]
        {
            "loke", "āpattiṃ", "kāyena", "dukkhaṃ", "bhikkhū",
            "dhammassa", "cittaṃ"
        };

        // Progression pattern: easy, hard, easy, easy, hard, hard (repeated)
        var pattern = new[] { true, false, true, true, false, false };

        var records = new List<IPracticeHistory>();
        var id = 1;
        var level = 3; // Starting level

        // Today's records (most recent first)
        for (var i = 0; i < todayForms.Length; i++)
        {
            var wasEasy = pattern[i % pattern.Length];
            var oldLevel = level;
            var newLevel = wasEasy ? Math.Min(level + 1, 10) : Math.Max(level - 1, 1);
            level = newLevel;

            records.Add(new MockPracticeHistory
            {
                Id = id++,
                FormId = 100000000 + i,
                FormText = todayForms[i],
                OldLevel = oldLevel,
                NewLevel = newLevel,
                PracticedUtc = now.AddMinutes(-5 * i)
            });
        }

        // Reset level for yesterday
        level = 2;

        // Yesterday's records
        for (var i = 0; i < yesterdayForms.Length; i++)
        {
            var wasEasy = pattern[i % pattern.Length];
            var oldLevel = level;
            var newLevel = wasEasy ? Math.Min(level + 1, 10) : Math.Max(level - 1, 1);
            level = newLevel;

            records.Add(new MockPracticeHistory
            {
                Id = id++,
                FormId = 100000100 + i,
                FormText = yesterdayForms[i],
                OldLevel = oldLevel,
                NewLevel = newLevel,
                PracticedUtc = yesterday8pm.AddMinutes(-5 * i)
            });
        }

        return records;
    }

    /// <summary>
    /// Creates 20 mock verb history records with high-EBT forms.
    /// </summary>
    static List<IPracticeHistory> CreateMockVerbHistory()
    {
        var now = DateTime.UtcNow;
        var yesterday8pm = now.Date.AddDays(-1).AddHours(20);

        // Forms for today (12 items) and yesterday (8 items)
        var todayForms = new[]
        {
            "hoti", "viharati", "bhavetha", "yāti", "pajānāti", "atthi",
            "vadati", "karoti", "vuccati", "gacchati", "uppajjati", "natthi"
        };
        var yesterdayForms = new[]
        {
            "eti", "passati", "jānāti", "maññati", "peti", "bhāti",
            "vadeti", "honti"
        };

        // Progression pattern: easy, hard, easy, easy, hard, hard (repeated)
        var pattern = new[] { true, false, true, true, false, false };

        var records = new List<IPracticeHistory>();
        var id = 1;
        var level = 3; // Starting level

        // Today's records (most recent first)
        for (var i = 0; i < todayForms.Length; i++)
        {
            var wasEasy = pattern[i % pattern.Length];
            var oldLevel = level;
            var newLevel = wasEasy ? Math.Min(level + 1, 10) : Math.Max(level - 1, 1);
            level = newLevel;

            records.Add(new MockPracticeHistory
            {
                Id = id++,
                FormId = 700000000 + i,
                FormText = todayForms[i],
                OldLevel = oldLevel,
                NewLevel = newLevel,
                PracticedUtc = now.AddMinutes(-5 * i)
            });
        }

        // Reset level for yesterday
        level = 2;

        // Yesterday's records
        for (var i = 0; i < yesterdayForms.Length; i++)
        {
            var wasEasy = pattern[i % pattern.Length];
            var oldLevel = level;
            var newLevel = wasEasy ? Math.Min(level + 1, 10) : Math.Max(level - 1, 1);
            level = newLevel;

            records.Add(new MockPracticeHistory
            {
                Id = id++,
                FormId = 700000100 + i,
                FormText = yesterdayForms[i],
                OldLevel = oldLevel,
                NewLevel = newLevel,
                PracticedUtc = yesterday8pm.AddMinutes(-5 * i)
            });
        }

        return records;
    }

    #endregion
#endif
}
