using System.Collections.ObjectModel;

namespace PaliPractice.Presentation.Practice.ViewModels;

/// <summary>
/// Navigation data wrapper for PracticeType (needed because DataViewMap requires reference types).
/// </summary>
public record HistoryNavigationData(PracticeType PracticeType);

/// <summary>
/// Represents a single practice history entry.
/// Will be moved to Models and connected to SQLite later.
/// </summary>
public class HistoryRecord
{
    [SQLite.PrimaryKey, SQLite.AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Type of practice (Declension or Conjugation).
    /// </summary>
    public PracticeType PracticeType { get; set; }

    /// <summary>
    /// Foreign key to either declension or conjugation form.
    /// </summary>
    public int FormId { get; set; }

    /// <summary>
    /// The actual form string (noun declension or verb conjugation).
    /// </summary>
    public string Form { get; set; } = string.Empty;

    /// <summary>
    /// Previous mastery value (0-100).
    /// </summary>
    public uint OldValue { get; set; }

    /// <summary>
    /// New mastery value after practice (0-100).
    /// </summary>
    public uint NewValue { get; set; }

    /// <summary>
    /// UNIX timestamp of when this practice occurred.
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Whether the user improved (NewValue > OldValue).
    /// </summary>
    public bool IsImproved => NewValue > OldValue;

    /// <summary>
    /// Progress percentage for display (0.0 to 1.0).
    /// </summary>
    public double Progress => NewValue / 100.0;
}

[Bindable]
public partial class HistoryViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public HistoryViewModel(INavigator navigator, HistoryNavigationData data)
    {
        _navigator = navigator;
        CurrentPracticeType = data.PracticeType;

        // Mock data - filtered by practice type
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var allRecords = new List<HistoryRecord>
        {
            // Declension records (nouns)
            new() { Id = 1, PracticeType = PracticeType.Declension, FormId = 101, Form = "dhammassa", OldValue = 60, NewValue = 75, Timestamp = now - 60 },
            new() { Id = 2, PracticeType = PracticeType.Declension, FormId = 102, Form = "buddhānaṃ", OldValue = 40, NewValue = 55, Timestamp = now - 120 },
            new() { Id = 4, PracticeType = PracticeType.Declension, FormId = 103, Form = "dhammesu", OldValue = 30, NewValue = 45, Timestamp = now - 240 },
            new() { Id = 6, PracticeType = PracticeType.Declension, FormId = 104, Form = "bhikkhuno", OldValue = 50, NewValue = 40, Timestamp = now - 360 },
            new() { Id = 8, PracticeType = PracticeType.Declension, FormId = 105, Form = "cittena", OldValue = 70, NewValue = 85, Timestamp = now - 480 },
            new() { Id = 10, PracticeType = PracticeType.Declension, FormId = 106, Form = "sīlāni", OldValue = 45, NewValue = 60, Timestamp = now - 600 },

            // Conjugation records (verbs)
            new() { Id = 3, PracticeType = PracticeType.Conjugation, FormId = 201, Form = "karoti", OldValue = 80, NewValue = 70, Timestamp = now - 180 },
            new() { Id = 5, PracticeType = PracticeType.Conjugation, FormId = 202, Form = "gacchati", OldValue = 90, NewValue = 95, Timestamp = now - 300 },
            new() { Id = 7, PracticeType = PracticeType.Conjugation, FormId = 203, Form = "passati", OldValue = 20, NewValue = 35, Timestamp = now - 420 },
            new() { Id = 9, PracticeType = PracticeType.Conjugation, FormId = 204, Form = "hoti", OldValue = 55, NewValue = 55, Timestamp = now - 540 },
            new() { Id = 11, PracticeType = PracticeType.Conjugation, FormId = 205, Form = "vadati", OldValue = 65, NewValue = 80, Timestamp = now - 660 },
            new() { Id = 12, PracticeType = PracticeType.Conjugation, FormId = 206, Form = "tiṭṭhati", OldValue = 35, NewValue = 25, Timestamp = now - 720 },
        };

        // Filter by practice type and order by timestamp (newest first)
        var filtered = allRecords
            .Where(r => r.PracticeType == CurrentPracticeType)
            .OrderByDescending(r => r.Timestamp);

        Records = new ObservableCollection<HistoryRecord>(filtered);
    }

    public PracticeType CurrentPracticeType { get; }

    public string Title => CurrentPracticeType == PracticeType.Declension
        ? "Declension History"
        : "Conjugation History";

    public ObservableCollection<HistoryRecord> Records { get; }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
}
