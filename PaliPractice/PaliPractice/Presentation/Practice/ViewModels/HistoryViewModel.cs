using System.Collections.ObjectModel;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;

namespace PaliPractice.Presentation.Practice.ViewModels;

/// <summary>
/// Navigation data wrapper for PracticeType (needed because DataViewMap requires reference types).
/// </summary>
public record HistoryNavigationData(PracticeType PracticeType);

[Bindable]
public partial class HistoryViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public HistoryViewModel(INavigator navigator, IUserDataService userData, HistoryNavigationData data)
    {
        _navigator = navigator;
        CurrentPracticeType = data.PracticeType;

        // Load real history from the database
        var history = userData.GetRecentHistory(data.PracticeType, limit: 50);
        Records = new ObservableCollection<PracticeHistory>(history);
    }

    public PracticeType CurrentPracticeType { get; }

    public string Title => CurrentPracticeType == PracticeType.Declension
        ? "Declension History"
        : "Conjugation History";

    public ObservableCollection<PracticeHistory> Records { get; }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
}
