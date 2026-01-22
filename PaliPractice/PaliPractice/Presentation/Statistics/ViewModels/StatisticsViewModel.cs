using System.Collections.ObjectModel;
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

            // Load calendar data for last 30 days
            var calendar = _db.Statistics.GetLast30DaysCalendar();
            CalendarData = new ObservableCollection<CalendarDayDto>(calendar);

            // Load type-specific stats
            NounStats = _db.Statistics.GetNounStats();
            VerbStats = _db.Statistics.GetVerbStats();
        });

        IsLoading = false;
    }

    [RelayCommand]
    async Task Refresh()
    {
        await LoadDataAsync();
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
}
