namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Manages flashcard reveal state for practice screens.
/// Controls when the answer is shown and tracks the current inflected form.
/// </summary>
[Bindable]
public partial class FlashcardStateViewModel : ObservableObject
{
    [ObservableProperty] bool _isRevealed;
    [ObservableProperty] string _answer = string.Empty;

    string _inflectedForm = string.Empty;

    /// <summary>
    /// Sets the inflected form that will be shown when revealed.
    /// Call this when a new card is displayed.
    /// </summary>
    public void SetAnswer(string inflectedForm)
    {
        _inflectedForm = inflectedForm;
    }

    /// <summary>
    /// Reveals the answer by showing the inflected form.
    /// </summary>
    public void Reveal()
    {
        Answer = _inflectedForm;
        IsRevealed = true;
    }

    /// <summary>
    /// Resets to hidden state for the next card.
    /// </summary>
    public void Reset()
    {
        IsRevealed = false;
        Answer = string.Empty;
        _inflectedForm = string.Empty;
    }
}
