using System.Collections.ObjectModel;
using PaliPractice.Services.Grammar;
using PaliPractice.Services.UserData.Entities;

namespace PaliPractice.Presentation.Practice.ViewModels;

/// <summary>
/// Navigation data wrapper for PracticeType (needed because DataViewMap requires reference types).
/// </summary>
public record HistoryNavigationData(PracticeType PracticeType);

[Bindable]
public class HistoryViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public HistoryViewModel(INavigator navigator, IDatabaseService db, IInflectionService inflection, HistoryNavigationData data)
    {
        _navigator = navigator;
        CurrentPracticeType = data.PracticeType;

        // Load history from database and resolve form text from FormId
        var history = db.UserData.GetRecentHistory(data.PracticeType, limit: 50);
        foreach (var record in history)
        {
            record.FormText = inflection.ResolveFormText(record.FormId, data.PracticeType) ?? "?";
        }
        Records = new ObservableCollection<IPracticeHistory>(history);
    }

    PracticeType CurrentPracticeType { get; }

    public string Title => CurrentPracticeType == PracticeType.Declension
        ? "Declension History"
        : "Conjugation History";

    public ObservableCollection<IPracticeHistory> Records { get; }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
}
