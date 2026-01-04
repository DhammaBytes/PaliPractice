using System.Collections.ObjectModel;
using PaliPractice.Services.Database;
using PaliPractice.Services.UserData.Statistics;

namespace PaliPractice.Presentation.Statistics.ViewModels;

[Bindable]
public partial class StatisticsViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly IDatabaseService _db;

    [ObservableProperty]
    bool _isLoading = true;

    // === General Section ===
    [ObservableProperty]
    GeneralStatsDto _general = new();

    [ObservableProperty]
    ObservableCollection<CalendarDayDto> _calendarData = [];

    [ObservableProperty]
    int _selectedYear;

    [ObservableProperty]
    int _selectedMonth;

    // === Noun Section ===
    [ObservableProperty]
    PracticeTypeStatsDto _nounStats = new();

    // === Verb Section ===
    [ObservableProperty]
    PracticeTypeStatsDto _verbStats = new();

    public StatisticsViewModel(INavigator navigator, IDatabaseService db)
    {
        _navigator = navigator;
        _db = db;

        var now = DateTime.Now;
        _selectedYear = now.Year;
        _selectedMonth = now.Month;

        // Load data asynchronously
        _ = LoadDataAsync();
    }

    async Task LoadDataAsync()
    {
        IsLoading = true;

        await Task.Run(() =>
        {
            // Load general stats
            General = _db.Statistics.GetGeneralStats();

            // Load calendar data for selected month
            var calendar = _db.Statistics.GetCalendarData(SelectedYear, SelectedMonth);
            CalendarData = new ObservableCollection<CalendarDayDto>(calendar);

            // Load type-specific stats
            NounStats = _db.Statistics.GetNounStats();
            VerbStats = _db.Statistics.GetVerbStats();
        });

        IsLoading = false;
    }

    [RelayCommand]
    void PreviousMonth()
    {
        if (SelectedMonth == 1)
        {
            SelectedMonth = 12;
            SelectedYear--;
        }
        else
        {
            SelectedMonth--;
        }
        RefreshCalendar();
    }

    [RelayCommand]
    void NextMonth()
    {
        var now = DateTime.Now;
        // Don't go beyond current month
        if (SelectedYear == now.Year && SelectedMonth == now.Month)
            return;

        if (SelectedMonth == 12)
        {
            SelectedMonth = 1;
            SelectedYear++;
        }
        else
        {
            SelectedMonth++;
        }
        RefreshCalendar();
    }

    void RefreshCalendar()
    {
        var calendar = _db.Statistics.GetCalendarData(SelectedYear, SelectedMonth);
        CalendarData = new ObservableCollection<CalendarDayDto>(calendar);
    }

    [RelayCommand]
    async Task Refresh()
    {
        await LoadDataAsync();
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    /// <summary>
    /// Gets the month name for the currently selected month.
    /// </summary>
    public string SelectedMonthName => new DateTime(SelectedYear, SelectedMonth, 1).ToString("MMMM yyyy");
}
