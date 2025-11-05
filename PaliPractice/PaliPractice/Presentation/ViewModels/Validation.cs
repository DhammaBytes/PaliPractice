namespace PaliPractice.Presentation.ViewModels;

public enum ValidationState { Unknown, Correct, Incorrect }

public interface IValidatableChoice
{
    bool HasSelection { get; }
    ValidationState Validation { get; }
    void Reset();
}
