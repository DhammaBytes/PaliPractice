namespace PaliPractice.Presentation.Main.ViewModels;

[Bindable]
public partial class HelpViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public HelpViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    // Section titles
    public const string HowToPracticeTitle = "How to Practice";
    public const string SpacedRepetitionTitle = "Spaced Repetition";
    public const string QuestionsTitle = "Questions & Answers";

    // Section content
    public static string HowToPractice => """
This app helps you memorize Pāli declensions and conjugations for nouns and verbs. You will see a Pāli word along with labels indicating the required form — such as case and number for nouns, or tense and person for verbs.

Try to recall the correct inflected form and its meaning. Then tap **Reveal** to see the answer along with possible translations. Finally, mark **Easy** or **Hard** based on how well you knew both.
""";

    public static string SpacedRepetition => """
The app tracks how well you know each form. Every form has a progress level from 1 to 10:

- **Level 1–3:** Still learning — reviewed within days
- **Level 4–6:** Becoming familiar — reviewed every week or two
- **Level 7–9:** Well known — reviewed monthly or less
- **Level 10:** Mastered — reviewed after about 8 months

All forms start at level 4. Marking **Easy** increases the level by 1; marking **Hard** decreases it by 1. This ensures you spend more time on forms you find difficult.
""";

    // FAQ - individual questions
    public static string FaqGrammar => """
**Where can I learn the grammar?**

This app is focused on memorizing, so it does not replace textbooks or courses. See the [Learn Pāli: Best way to start?](https://palistudies.blogspot.com/2018/05/learn-pali-where-to-begin.html) guide and its author's [Learn Pāli Language series](https://www.youtube.com/playlist?list=PLf6RXFuRpeLRYj-wWs4KFrTPrzvAWWMpo) on YouTube.
""";

    public static string FaqMultipleForms => """
**Why do some answers show multiple forms?**

Certain cases and tenses in Pāli have more than one valid ending. The app displays all used forms for the given grammatical context (and skips potentially valid forms that are not found in the Pāli corpus).
""";

    public static string FaqExamples => """
**Why do sutta examples use different forms?**

Example sentences come from the [Digital Pāli Dictionary](https://dpdict.net), which provides 1–2 examples per word meaning rather than per inflected form. These examples illustrate how the word is used in context, not necessarily the specific form being practiced.
""";

    public static string FaqMissingWords => """
**Why are some words missing?**

This app focuses on forms most useful for beginner-to-intermediate students. It includes the 1,500 most frequent nouns and 750 most frequent verbs from the Pāli Canon. Some proper names and complex compound words have been excluded. For extended vocabulary and grammar reference, please consult the [Digital Pāli Dictionary](https://dpdict.net).
""";

    public static string FaqMissingForms => """
**Why are aorist and participle forms not included?**

These forms vary considerably across verbs and often require learning on a word-by-word basis. This does not fit well within the current practice format. A dedicated trainer for these forms may be added in future versions.
""";

    public static string FaqAboutApp => """
**Where can I get more information about the app?**

See the About section in Settings for details about the app, its data sources, licensing, and how to contact us.
""";

    public static string FaqDataStorage => """
**Where is my data stored?**

This app works completely offline and does not back up your settings or practice progress, nor does it sync data between devices.

On mobile devices, your phone's system backup typically includes all local app data automatically. On desktop, the data is stored in typical app configuration directories and needs to be backed up manually.
""";
}
